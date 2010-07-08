/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Apache.NMS.Stomp.Commands;
using Apache.NMS.Stomp.Util;
using Apache.NMS;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp
{
    public enum AckType
    {
        ConsumedAck = 1, // Message consumed, discard
        IndividualAck = 2 // Only the given message is to be treated as consumed.
    }

    /// <summary>
    /// An object capable of receiving messages from some destination
    /// </summary>
    public class MessageConsumer : IMessageConsumer, IDispatcher
    {
        private readonly MessageDispatchChannel unconsumedMessages = new MessageDispatchChannel();
        private readonly LinkedList<MessageDispatch> dispatchedMessages = new LinkedList<MessageDispatch>();
        private readonly ConsumerInfo info;
        private Session session;

        private MessageAck pendingAck = null;

        private Atomic<bool> started = new Atomic<bool>();
        private Atomic<bool> deliveringAcks = new Atomic<bool>();

        protected bool disposed = false;
        private int deliveredCounter = 0;
        private int additionalWindowSize = 0;
        private long redeliveryDelay = 0;
        private int dispatchedCount = 0;
        private volatile bool synchronizationRegistered = false;
        private bool clearDispatchList = false;
        private bool inProgressClearRequiredFlag;

        private event MessageListener listener;

        private IRedeliveryPolicy redeliveryPolicy;

        // Constructor internal to prevent clients from creating an instance.
        internal MessageConsumer(Session session, ConsumerInfo info)
        {
            this.session = session;
            this.info = info;
            this.redeliveryPolicy = this.session.Connection.RedeliveryPolicy;
        }

        ~MessageConsumer()
        {
            Dispose(false);
        }

        #region Property Accessors

        public ConsumerId ConsumerId
        {
            get { return info.ConsumerId; }
        }

        public int PrefetchSize
        {
            get { return this.info.PrefetchSize; }
        }

        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.redeliveryPolicy; }
            set { this.redeliveryPolicy = value; }
        }

        #endregion

        #region IMessageConsumer Members

        public event MessageListener Listener
        {
            add
            {
                CheckClosed();

                if(this.PrefetchSize == 0)
                {
                    throw new NMSException("Cannot set Asynchronous Listener on a Consumer with a zero Prefetch size");
                }

                bool wasStarted = this.session.Started;

                if(wasStarted == true)
                {
                    this.session.Stop();
                }

                listener += value;
                this.session.Redispatch(this.unconsumedMessages);

                if(wasStarted == true)
                {
                    this.session.Start();
                }
            }
            remove { listener -= value; }
        }

        public IMessage Receive()
        {
            CheckClosed();
            CheckMessageListener();

            MessageDispatch dispatch = this.Dequeue(TimeSpan.FromMilliseconds(-1));

            if(dispatch == null)
            {
                return null;
            }

            BeforeMessageIsConsumed(dispatch);
            AfterMessageIsConsumed(dispatch, false);

            return CreateStompMessage(dispatch);
        }

        public IMessage Receive(TimeSpan timeout)
        {
            CheckClosed();
            CheckMessageListener();

            MessageDispatch dispatch = this.Dequeue(timeout);

            if(dispatch == null)
            {
                return null;
            }

            BeforeMessageIsConsumed(dispatch);
            AfterMessageIsConsumed(dispatch, false);

            return CreateStompMessage(dispatch);
        }

        public IMessage ReceiveNoWait()
        {
            CheckClosed();
            CheckMessageListener();

            MessageDispatch dispatch = this.Dequeue(TimeSpan.Zero);

            if(dispatch == null)
            {
                return null;
            }

            BeforeMessageIsConsumed(dispatch);
            AfterMessageIsConsumed(dispatch, false);

            return CreateStompMessage(dispatch);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(disposed)
            {
                return;
            }

            if(disposing)
            {
                // Dispose managed code here.
            }

            try
            {
                Close();
            }
            catch
            {
                // Ignore network errors.
            }

            disposed = true;
        }

        public void Close()
        {
            if(!this.unconsumedMessages.Closed)
            {
                if(this.session.IsTransacted && this.session.TransactionContext.InTransaction)
                {
                    this.session.TransactionContext.AddSynchronization(new ConsumerCloseSynchronization(this));
                }
                else
                {
                    this.DoClose();
                }
            }
        }

        internal void DoClose()
        {
            if(!this.unconsumedMessages.Closed)
            {
                Tracer.Debug("Closing down the Consumer");

                if(!this.session.IsTransacted)
                {
                    lock(this.dispatchedMessages)
                    {
                        dispatchedMessages.Clear();
                    }
                }

                this.unconsumedMessages.Close();
                this.session.DisposeOf(this.info.ConsumerId);

                RemoveInfo removeCommand = new RemoveInfo();
                removeCommand.ObjectId = this.info.ConsumerId;

                this.session.Connection.Oneway(removeCommand);
                this.session = null;

                Tracer.Debug("Consumer instnace Closed.");
            }
        }

        #endregion

        protected void DoIndividualAcknowledge(Message message)
        {
            MessageDispatch dispatch = null;

            lock(this.dispatchedMessages)
            {
                foreach(MessageDispatch originalDispatch in this.dispatchedMessages)
                {
                    if(originalDispatch.Message.MessageId.Equals(message.MessageId))
                    {
                        dispatch = originalDispatch;
                        this.dispatchedMessages.Remove(originalDispatch);
                        break;
                    }
                }
            }

            if(dispatch == null)
            {
                Tracer.DebugFormat("Attempt to Ack MessageId[{0}] failed because the original dispatch is not in the Dispatch List", message.MessageId);
                return;
            }

            MessageAck ack = new MessageAck();

            ack.AckType = (byte) AckType.IndividualAck;
            ack.ConsumerId = this.info.ConsumerId;
            ack.Destination = dispatch.Destination;
            ack.LastMessageId = dispatch.Message.MessageId;
            ack.MessageCount = 1;

            Tracer.Debug("Sending Individual Ack for MessageId: " + ack.LastMessageId.ToString());
            this.session.SendAck(ack);
        }

        protected void DoNothingAcknowledge(Message message)
        {
        }

        protected void DoClientAcknowledge(Message message)
        {
            this.CheckClosed();
            Tracer.Debug("Sending Client Ack:");
            this.session.Acknowledge();
        }

        public void Start()
        {
            if(this.unconsumedMessages.Closed)
            {
                return;
            }

            this.started.Value = true;
            this.unconsumedMessages.Start();
            this.session.Executor.Wakeup();
        }

        public void Stop()
        {
            this.started.Value = false;
            this.unconsumedMessages.Stop();
        }

        internal void InProgressClearRequired()
        {
            inProgressClearRequiredFlag = true;
            // deal with delivered messages async to avoid lock contention with in progress acks
            clearDispatchList = true;
        }

        internal void ClearMessagesInProgress()
        {
            if(inProgressClearRequiredFlag)
            {
                // Called from a thread in the ThreadPool, so we wait until we can
                // get a lock on the unconsumed list then we clear it.
                lock(this.unconsumedMessages)
                {
                    if(inProgressClearRequiredFlag)
                    {
                        if(Tracer.IsDebugEnabled)
                        {
                            Tracer.Debug(this.ConsumerId + " clearing dispatched list (" +
                                         this.unconsumedMessages.Count + ") on transport interrupt");
                        }

                        this.unconsumedMessages.Clear();
                        this.synchronizationRegistered = false;

                        // allow dispatch on this connection to resume
                        this.session.Connection.TransportInterruptionProcessingComplete();
                        this.inProgressClearRequiredFlag = false;
                    }
                }
            }
        }

        public void DeliverAcks()
        {
            MessageAck ack = null;

            if(this.deliveringAcks.CompareAndSet(false, true))
            {
                if(pendingAck != null && pendingAck.AckType == (byte) AckType.ConsumedAck)
                {
                    ack = pendingAck;
                    pendingAck = null;
                }

                if(pendingAck != null)
                {
                    MessageAck ackToSend = ack;

                    try
                    {
                        this.session.SendAck(ackToSend);
                    }
                    catch(Exception e)
                    {
                        Tracer.DebugFormat("{0} : Failed to send ack, {1}", this.info.ConsumerId, e);
                    }
                }
                else
                {
                    this.deliveringAcks.Value = false;
                }
            }
        }

        public void Dispatch(MessageDispatch dispatch)
        {
            MessageListener listener = this.listener;

            try
            {
                lock(this.unconsumedMessages.SyncRoot)
                {
                    if(this.clearDispatchList)
                    {
                        // we are reconnecting so lets flush the in progress messages
                        this.clearDispatchList = false;
                        this.unconsumedMessages.Clear();

                        if(this.pendingAck != null)
                        {
                            // on resumption a pending delivered ack will be out of sync with
                            // re-deliveries.
                            if(Tracer.IsDebugEnabled)
                            {
                                Tracer.Debug("removing pending delivered ack on transport interupt: " + pendingAck);
                            }
                            this.pendingAck = null;
                        }
                    }

                    if(!this.unconsumedMessages.Closed)
                    {
                        if(listener != null && this.unconsumedMessages.Running)
                        {
                            Message message = CreateStompMessage(dispatch);

                            this.BeforeMessageIsConsumed(dispatch);

                            try
                            {
                                bool expired = message.IsExpired();

                                if(!expired)
                                {
                                    listener(message);
                                }

                                this.AfterMessageIsConsumed(dispatch, expired);
                            }
                            catch(Exception e)
                            {
                                if(this.session.IsAutoAcknowledge || this.session.IsIndividualAcknowledge)
                                {
                                    // Redeliver the message
                                }
                                else
                                {
                                    // Transacted or Client ack: Deliver the next message.
                                    this.AfterMessageIsConsumed(dispatch, false);
                                }

                                Tracer.Error(this.info.ConsumerId + " Exception while processing message: " + e);
                            }
                        }
                        else
                        {
                            this.unconsumedMessages.Enqueue(dispatch);
                        }
                    }
                }

                if(++dispatchedCount % 1000 == 0)
                {
                    dispatchedCount = 0;
                    Thread.Sleep((int)1);
                }
            }
            catch(Exception e)
            {
                this.session.Connection.OnSessionException(this.session, e);
            }
        }

        public bool Iterate()
        {
            if(this.listener != null)
            {
                MessageDispatch dispatch = this.unconsumedMessages.DequeueNoWait();
                if(dispatch != null)
                {
                    try
                    {
                        Message message = CreateStompMessage(dispatch);
                        BeforeMessageIsConsumed(dispatch);
                        listener(message);
                        AfterMessageIsConsumed(dispatch, false);
                    }
                    catch(NMSException e)
                    {
                        this.session.Connection.OnSessionException(this.session, e);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Used to get an enqueued message from the unconsumedMessages list. The
        /// amount of time this method blocks is based on the timeout value.  if
        /// timeout == Timeout.Infinite then it blocks until a message is received.
        /// if timeout == 0 then it it tries to not block at all, it returns a
        /// message if it is available if timeout > 0 then it blocks up to timeout
        /// amount of time.  Expired messages will consumed by this method.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="System.TimeSpan"/>
        /// </param>
        /// <returns>
        /// A <see cref="MessageDispatch"/>
        /// </returns>
        private MessageDispatch Dequeue(TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            while(true)
            {
                MessageDispatch dispatch = this.unconsumedMessages.Dequeue(timeout);

                // Grab a single date/time for calculations to avoid timing errors.
                DateTime dispatchTime = DateTime.Now;

                if(dispatch == null)
                {
                    if(timeout > TimeSpan.Zero && !this.unconsumedMessages.Closed)
                    {
                        if(dispatchTime > deadline)
                        {
                            // Out of time.
                            timeout = TimeSpan.Zero;
                        }
                        else
                        {
                            // Adjust the timeout to the remaining time.
                            timeout = deadline - dispatchTime;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else if(dispatch.Message == null)
                {
                    return null;
                }
                else if(dispatch.Message.IsExpired())
                {
                    Tracer.DebugFormat("{0} received expired message: {1}", info.ConsumerId, dispatch.Message.MessageId);

                    BeforeMessageIsConsumed(dispatch);
                    AfterMessageIsConsumed(dispatch, true);
                    // Refresh the dispatch time
                    dispatchTime = DateTime.Now;

                    if(timeout > TimeSpan.Zero && !this.unconsumedMessages.Closed)
                    {
                        if(dispatchTime > deadline)
                        {
                            // Out of time.
                            timeout = TimeSpan.Zero;
                        }
                        else
                        {
                            // Adjust the timeout to the remaining time.
                            timeout = deadline - dispatchTime;
                        }
                    }
                }
                else
                {
                    return dispatch;
                }
            }
        }

        public void BeforeMessageIsConsumed(MessageDispatch dispatch)
        {
            if(!this.session.IsAutoAcknowledge)
            {
                lock(this.dispatchedMessages)
                {
                    this.dispatchedMessages.AddFirst(dispatch);
                }

                if(this.session.IsTransacted)
                {
                    this.AckLater(dispatch);
                }
            }
        }

        public void AfterMessageIsConsumed(MessageDispatch dispatch, bool expired)
        {
            if(this.unconsumedMessages.Closed)
            {
                return;
            }

            if(expired == true)
            {
                lock(this.dispatchedMessages)
                {
                    this.dispatchedMessages.Remove(dispatch);
                }

                // TODO - Not sure if we need to ack this in stomp.
                // AckLater(dispatch, AckType.ConsumedAck);
            }
            else
            {
                if(this.session.IsTransacted)
                {
                    // Do nothing.
                }
                else if(this.session.IsAutoAcknowledge)
                {
                    if(this.deliveringAcks.CompareAndSet(false, true))
                    {
                        lock(this.dispatchedMessages)
                        {
                            MessageAck ack = new MessageAck();

                            ack.AckType = (byte) AckType.ConsumedAck;
                            ack.ConsumerId = this.info.ConsumerId;
                            ack.Destination = dispatch.Destination;
                            ack.LastMessageId = dispatch.Message.MessageId;
                            ack.MessageCount = 1;

                            this.session.SendAck(ack);
                        }

                        this.deliveringAcks.Value = false;
                    }
                }
                else if(this.session.IsClientAcknowledge || this.session.IsIndividualAcknowledge)
                {
                    // Do nothing.
                }
                else
                {
                    throw new NMSException("Invalid session state.");
                }
            }
        }

        private MessageAck MakeAckForAllDeliveredMessages()
        {
            lock(this.dispatchedMessages)
            {
                if(this.dispatchedMessages.Count == 0)
                {
                    return null;
                }

                MessageDispatch dispatch = this.dispatchedMessages.First.Value;
                MessageAck ack = new MessageAck();

                ack.AckType = (byte) AckType.ConsumedAck;
                ack.ConsumerId = this.info.ConsumerId;
                ack.Destination = dispatch.Destination;
                ack.LastMessageId = dispatch.Message.MessageId;
                ack.MessageCount = this.dispatchedMessages.Count;
                ack.FirstMessageId = this.dispatchedMessages.Last.Value.Message.MessageId;

                return ack;
            }
        }

        private void AckLater(MessageDispatch dispatch)
        {
            // Don't acknowledge now, but we may need to let the broker know the
            // consumer got the message to expand the pre-fetch window
            if(this.session.IsTransacted)
            {
                this.session.DoStartTransaction();

                if(!synchronizationRegistered)
                {
                    this.synchronizationRegistered = true;
                    this.session.TransactionContext.AddSynchronization(new MessageConsumerSynchronization(this));
                }
            }

            this.deliveredCounter++;

            MessageAck oldPendingAck = pendingAck;

            pendingAck = new MessageAck();
            pendingAck.AckType = (byte) AckType.ConsumedAck;
            pendingAck.ConsumerId = this.info.ConsumerId;
            pendingAck.Destination = dispatch.Destination;
            pendingAck.LastMessageId = dispatch.Message.MessageId;
            pendingAck.MessageCount = deliveredCounter;

            if(this.session.IsTransacted && this.session.TransactionContext.InTransaction)
            {
                pendingAck.TransactionId = this.session.TransactionContext.TransactionId;
            }

            if(oldPendingAck == null)
            {
                pendingAck.FirstMessageId = pendingAck.LastMessageId;
            }

            if((0.5 * this.info.PrefetchSize) <= (this.deliveredCounter - this.additionalWindowSize))
            {
                this.session.SendAck(pendingAck);
                this.pendingAck = null;
                this.deliveredCounter = 0;
                this.additionalWindowSize = 0;
            }
        }

        internal void Acknowledge()
        {
            lock(this.dispatchedMessages)
            {
                // Acknowledge all messages so far.
                MessageAck ack = MakeAckForAllDeliveredMessages();

                if(ack == null)
                {
                    return; // no msgs
                }

                if(this.session.IsTransacted)
                {
                    this.session.DoStartTransaction();
                    ack.TransactionId = this.session.TransactionContext.TransactionId;
                }

                this.session.SendAck(ack);
                this.pendingAck = null;

                // Adjust the counters
                this.deliveredCounter = Math.Max(0, this.deliveredCounter - this.dispatchedMessages.Count);
                this.additionalWindowSize = Math.Max(0, this.additionalWindowSize - this.dispatchedMessages.Count);

                if(!this.session.IsTransacted)
                {
                    this.dispatchedMessages.Clear();
                }
            }
        }

        private void Commit()
        {
            lock(this.dispatchedMessages)
            {
                this.dispatchedMessages.Clear();
            }

            this.redeliveryDelay = 0;
        }

        private void Rollback()
        {
            lock(this.unconsumedMessages.SyncRoot)
            {
                lock(this.dispatchedMessages)
                {
                    if(this.dispatchedMessages.Count == 0)
                    {
                        return;
                    }

                    // Only increase the redelivery delay after the first redelivery..
                    MessageDispatch lastMd = this.dispatchedMessages.First.Value;
                    int currentRedeliveryCount = lastMd.Message.RedeliveryCounter;

                    redeliveryDelay = this.redeliveryPolicy.RedeliveryDelay(currentRedeliveryCount);

                    foreach(MessageDispatch dispatch in this.dispatchedMessages)
                    {
                        // Allow the message to update its internal to reflect a Rollback.
                        dispatch.Message.OnMessageRollback();
                    }

                    if(this.redeliveryPolicy.MaximumRedeliveries >= 0 &&
                       lastMd.Message.RedeliveryCounter > this.redeliveryPolicy.MaximumRedeliveries)
                    {
                        this.redeliveryDelay = 0;
                    }
                    else
                    {
                        // stop the delivery of messages.
                        this.unconsumedMessages.Stop();

                        foreach(MessageDispatch dispatch in this.dispatchedMessages)
                        {
                            this.unconsumedMessages.EnqueueFirst(dispatch);
                        }

                        if(redeliveryDelay > 0 && !this.unconsumedMessages.Closed)
                        {
                            DateTime deadline = DateTime.Now.AddMilliseconds(redeliveryDelay);
                            ThreadPool.QueueUserWorkItem(this.RollbackHelper, deadline);
                        }
                        else
                        {
                            Start();
                        }
                    }

                    this.deliveredCounter -= this.dispatchedMessages.Count;
                    this.dispatchedMessages.Clear();
                }
            }

            // Only redispatch if there's an async listener otherwise a synchronous
            // consumer will pull them from the local queue.
            if(this.listener != null)
            {
                this.session.Redispatch(this.unconsumedMessages);
            }
        }

        private void RollbackHelper(Object arg)
        {
            try
            {
                TimeSpan waitTime = (DateTime) arg - DateTime.Now;

                if(waitTime.CompareTo(TimeSpan.Zero) > 0)
                {
                    Thread.Sleep((int)waitTime.TotalMilliseconds);
                }

                this.Start();
            }
            catch(Exception e)
            {
                if(!this.unconsumedMessages.Closed)
                {
                    this.session.Connection.OnSessionException(this.session, e);
                }
            }
        }

        private Message CreateStompMessage(MessageDispatch dispatch)
        {
            Message message = dispatch.Message.Clone() as Message;

            message.Connection = this.session.Connection;

            if(this.session.IsClientAcknowledge)
            {
                message.Acknowledger += new AcknowledgeHandler(DoClientAcknowledge);
            }
            else if(this.session.IsIndividualAcknowledge)
            {
                message.Acknowledger += new AcknowledgeHandler(DoIndividualAcknowledge);
            }
            else
            {
                message.Acknowledger += new AcknowledgeHandler(DoNothingAcknowledge);
            }

            return message;
        }

        private void CheckClosed()
        {
            if(this.unconsumedMessages.Closed)
            {
                throw new NMSException("The Consumer has been Closed");
            }
        }

        private void CheckMessageListener()
        {
            if(this.listener != null)
            {
                throw new NMSException("Cannot set Async listeners on Consumers with a prefetch limit of zero");
            }
        }

        #region Nested ISyncronization Types

        class MessageConsumerSynchronization : ISynchronization
        {
            private readonly MessageConsumer consumer;

            public MessageConsumerSynchronization(MessageConsumer consumer)
            {
                this.consumer = consumer;
            }

            public void BeforeEnd()
            {
                this.consumer.Acknowledge();
                this.consumer.synchronizationRegistered = false;
            }

            public void AfterCommit()
            {
                this.consumer.Commit();
                this.consumer.synchronizationRegistered = false;
            }

            public void AfterRollback()
            {
                this.consumer.Rollback();
                this.consumer.synchronizationRegistered = false;
            }
        }

        class ConsumerCloseSynchronization : ISynchronization
        {
            private readonly MessageConsumer consumer;

            public ConsumerCloseSynchronization(MessageConsumer consumer)
            {
                this.consumer = consumer;
            }

            public void BeforeEnd()
            {
            }

            public void AfterCommit()
            {
                this.consumer.DoClose();
            }

            public void AfterRollback()
            {
                this.consumer.DoClose();
            }
        }

        #endregion
    }
}
