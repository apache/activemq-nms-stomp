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
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using Apache.NMS.Stomp.Commands;
using Apache.NMS.Stomp.Util;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp
{
    /// <summary>
    /// Default provider of ISession
    /// </summary>
    public class Session : ISession, IDispatcher
    {
        /// <summary>
        /// Private object used for synchronization, instead of public "this"
        /// </summary>
        private readonly object myLock = new object();

        private readonly IDictionary consumers = Hashtable.Synchronized(new Hashtable());
        private readonly IDictionary producers = Hashtable.Synchronized(new Hashtable());

        private readonly SessionExecutor executor;
        private readonly TransactionContext transactionContext;
        private Connection connection;

        private bool dispatchAsync;
        private bool exclusive;
        private bool retroactive;
        private byte priority;

        private readonly SessionInfo info;
        private int consumerCounter;
        private int producerCounter;
        private int nextDeliveryId;
        private bool disposed = false;
        private bool closed = false;
        private bool closing = false;
        private TimeSpan requestTimeout;
        private readonly AcknowledgementMode acknowledgementMode;

        public Session(Connection connection, SessionInfo info, AcknowledgementMode acknowledgementMode, bool dispatchAsync)
        {
            this.connection = connection;
            this.info = info;
            this.acknowledgementMode = acknowledgementMode;
            this.requestTimeout = connection.RequestTimeout;
            this.dispatchAsync = dispatchAsync;

            if(acknowledgementMode == AcknowledgementMode.Transactional)
            {
                this.transactionContext = new TransactionContext(this);
            }
            else if(acknowledgementMode == AcknowledgementMode.DupsOkAcknowledge)
            {
                this.acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
            }

            this.executor = new SessionExecutor(this, this.consumers);
        }

        ~Session()
        {
            Dispose(false);
        }

        #region Session Transaction Events

        // We delegate the events to the TransactionContext since it knows
        // what the state is at all times.

        public event SessionTxEventDelegate TransactionStartedListener
        {
            add { this.transactionContext.TransactionStartedListener += value; }
            remove { this.transactionContext.TransactionStartedListener += value; }
        }

        public event SessionTxEventDelegate TransactionCommittedListener
        {
            add { this.transactionContext.TransactionCommittedListener += value; }
            remove { this.transactionContext.TransactionCommittedListener += value; }
        }

        public event SessionTxEventDelegate TransactionRolledBackListener
        {
            add { this.transactionContext.TransactionRolledBackListener += value; }
            remove { this.transactionContext.TransactionRolledBackListener += value; }
        }

        #endregion

        #region Property Accessors

        /// <summary>
        /// Sets the prefetch size, the maximum number of messages a broker will dispatch to consumers
        /// until acknowledgements are received.
        /// </summary>
        public int PrefetchSize
        {
            set{ this.connection.PrefetchPolicy.SetAll(value); }
        }

        /// <summary>
        /// Sets the maximum number of messages to keep around per consumer
        /// in addition to the prefetch window for non-durable topics until messages
        /// will start to be evicted for slow consumers.
        /// Must be > 0 to enable this feature
        /// </summary>
        public int MaximumPendingMessageLimit
        {
            set{ this.connection.PrefetchPolicy.MaximumPendingMessageLimit = value; }
        }

        /// <summary>
        /// Enables or disables whether asynchronous dispatch should be used by the broker
        /// </summary>
        public bool DispatchAsync
        {
            get{ return this.dispatchAsync; }
            set{ this.dispatchAsync = value; }
        }

        /// <summary>
        /// Enables or disables exclusive consumers when using queues. An exclusive consumer means
        /// only one instance of a consumer is allowed to process messages on a queue to preserve order
        /// </summary>
        public bool Exclusive
        {
            get{ return this.exclusive; }
            set{ this.exclusive = value; }
        }

        /// <summary>
        /// Enables or disables retroactive mode for consumers; i.e. do they go back in time or not?
        /// </summary>
        public bool Retroactive
        {
            get{ return this.retroactive; }
            set{ this.retroactive = value; }
        }

        /// <summary>
        /// Sets the default consumer priority for consumers
        /// </summary>
        public byte Priority
        {
            get{ return this.priority; }
            set{ this.priority = value; }
        }

        public Connection Connection
        {
            get { return this.connection; }
        }

        public SessionId SessionId
        {
            get { return info.SessionId; }
        }

        public TransactionContext TransactionContext
        {
            get { return this.transactionContext; }
        }

        public TimeSpan RequestTimeout
        {
            get { return this.requestTimeout; }
            set { this.requestTimeout = value; }
        }

        public bool Transacted
        {
            get { return this.AcknowledgementMode == AcknowledgementMode.Transactional; }
        }

        public AcknowledgementMode AcknowledgementMode
        {
            get { return this.acknowledgementMode; }
        }

        public bool IsClientAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.ClientAcknowledge; }
        }

        public bool IsAutoAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.AutoAcknowledge; }
        }

        public bool IsDupsOkAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.DupsOkAcknowledge; }
        }

        public bool IsIndividualAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.IndividualAcknowledge; }
        }

        public bool IsTransacted
        {
            get { return this.acknowledgementMode == AcknowledgementMode.Transactional; }
        }

        public SessionExecutor Executor
        {
            get { return this.executor; }
        }

        public long NextDeliveryId
        {
            get { return Interlocked.Increment(ref this.nextDeliveryId); }
        }

        private ConsumerTransformerDelegate consumerTransformer;
        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumerTransformer; }
            set { this.consumerTransformer = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.producerTransformer; }
            set { this.producerTransformer = value; }
        }

        #endregion

        #region ISession Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(this.disposed)
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

            this.disposed = true;
        }

        public void Close()
        {
            lock(myLock)
            {
                if(this.closed)
                {
                    return;
                }

                try
                {
                    Tracer.InfoFormat("Closing The Session with Id {0}", this.info.SessionId.ToString());
                    DoClose();
                    Tracer.InfoFormat("Closed The Session with Id {0}", this.info.SessionId.ToString());
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Error during session close: {0}", ex);
                }
                finally
                {
                    this.connection = null;
                    this.closed = true;
                    this.closing = false;
                }
            }
        }

        internal void DoClose()
        {
            lock(myLock)
            {
                if(this.closed)
                {
                    return;
                }

                try
                {
                    this.closing = true;

                    // Stop all message deliveries from this Session
                    Stop();

                    lock(consumers.SyncRoot)
                    {
                        foreach(MessageConsumer consumer in consumers.Values)
                        {
                            consumer.FailureError = this.connection.FirstFailureError;
                            consumer.DoClose();
                        }
                    }
                    consumers.Clear();

                    lock(producers.SyncRoot)
                    {
                        foreach(MessageProducer producer in producers.Values)
                        {
                            producer.DoClose();
                        }
                    }
                    producers.Clear();

                    // If in a transaction roll it back
                    if(this.IsTransacted && this.transactionContext.InTransaction)
                    {
                        try
                        {
                            this.transactionContext.Rollback();
                        }
                        catch
                        {
                        }
                    }

                    Connection.RemoveSession(this);
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Error during session close: {0}", ex);
                }
                finally
                {
                    this.closed = true;
                    this.closing = false;
                }
            }
        }

        public IMessageProducer CreateProducer()
        {
            return CreateProducer(null);
        }

        public IMessageProducer CreateProducer(IDestination destination)
        {
            ProducerInfo command = CreateProducerInfo(destination);
            ProducerId producerId = command.ProducerId;
            MessageProducer producer = null;

            try
            {
                producer = new MessageProducer(this, command);
                producer.ProducerTransformer = this.ProducerTransformer;
                producers[producerId] = producer;
            }
            catch(Exception)
            {
                if(producer != null)
                {
                    producer.Close();
                }

                throw;
            }

            return producer;
        }

        public IMessageConsumer CreateConsumer(IDestination destination)
        {
            return CreateConsumer(destination, null, false);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        {
            return CreateConsumer(destination, selector, false);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            if (destination == null)
            {
                throw new InvalidDestinationException("Cannot create a Consumer with a Null destination");
            }

			int prefetchSize = this.Connection.PrefetchPolicy.DurableTopicPrefetch;

			if(destination.IsTopic)
			{
				prefetchSize = this.connection.PrefetchPolicy.TopicPrefetch;
			}
			else if(destination.IsQueue)
			{
				prefetchSize = this.connection.PrefetchPolicy.QueuePrefetch;
			}
			
            MessageConsumer consumer = null;

            try
            {
	            Destination dest = destination as Destination;
				consumer = new MessageConsumer(this, GetNextConsumerId(), dest, null, selector, prefetchSize, noLocal);
                consumer.ConsumerTransformer = this.ConsumerTransformer;
				this.AddConsumer(consumer);

				// lets register the consumer first in case we start dispatching messages immediately
				this.Connection.SyncRequest(consumer.ConsumerInfo);

				if(this.Started)
                {
                    consumer.Start();
                }
            }
            catch(Exception)
            {
                if(consumer != null)
                {
					this.RemoveConsumer(consumer);
                    consumer.Close();
                }

                throw;
            }

			return consumer;
		}

        public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
        {
            if (destination == null)
            {
                throw new InvalidDestinationException("Cannot create a Consumer with a Null destination");
            }

            MessageConsumer consumer = null;

            try
            {
				Destination dest = destination as Destination;
				consumer = new MessageConsumer(this, GetNextConsumerId(), dest, name, selector, this.connection.PrefetchPolicy.DurableTopicPrefetch, noLocal);
				consumer.ConsumerTransformer = this.ConsumerTransformer;
				this.AddConsumer(consumer);
				this.connection.SyncRequest(consumer.ConsumerInfo);

				if(this.Started)
                {
                    consumer.Start();
                }
            }
            catch(Exception)
            {
                if(consumer != null)
                {
					this.RemoveConsumer(consumer);
                    consumer.Close();
                }

                throw;
            }

            return consumer;
        }

        public void DeleteDurableConsumer(string name)
        {
            RemoveSubscriptionInfo command = new RemoveSubscriptionInfo();
            command.ConnectionId = Connection.ConnectionId;
            command.ClientId = Connection.ClientId;
            command.SubscriptionName = name;
            this.connection.SyncRequest(command);
        }

        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            throw new NotSupportedException("Not supported with Stomp Protocol");
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            throw new NotSupportedException("Not supported with Stomp Protocol");
        }

        public IQueue GetQueue(string name)
        {
            return new Commands.Queue(name);
        }

        public ITopic GetTopic(string name)
        {
            return new Commands.Topic(name);
        }

        public ITemporaryQueue CreateTemporaryQueue()
        {
            TempQueue answer = new TempQueue(Connection.CreateTemporaryDestinationName());
            return answer;
        }

        public ITemporaryTopic CreateTemporaryTopic()
        {
            TempTopic answer = new TempTopic(Connection.CreateTemporaryDestinationName());
            return answer;
        }

        /// <summary>
        /// Delete a destination (Queue, Topic, Temp Queue, Temp Topic).
        /// </summary>
        public void DeleteDestination(IDestination destination)
        {
            throw new NotSupportedException("Stomp Cannot delete Destinations");
        }

        public IMessage CreateMessage()
        {
            Message answer = new Message();
            return ConfigureMessage(answer) as IMessage;
        }

        public ITextMessage CreateTextMessage()
        {
            TextMessage answer = new TextMessage();
            return ConfigureMessage(answer) as ITextMessage;
        }

        public ITextMessage CreateTextMessage(string text)
        {
            TextMessage answer = new TextMessage(text);
            return ConfigureMessage(answer) as ITextMessage;
        }

        public IMapMessage CreateMapMessage()
        {
            MapMessage answer = new MapMessage();
            return ConfigureMessage(answer) as IMapMessage;
        }

        public IBytesMessage CreateBytesMessage()
        {
            return ConfigureMessage(new BytesMessage()) as IBytesMessage;
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            BytesMessage answer = new BytesMessage();
            answer.Content = body;
            return ConfigureMessage(answer) as IBytesMessage;
        }

        public IStreamMessage CreateStreamMessage()
        {
            throw new NotSupportedException("No Object Message in Stomp");
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            throw new NotSupportedException("No Object Message in Stomp");
        }

        public void Commit()
        {
            if(!Transacted)
            {
                throw new InvalidOperationException(
                        "You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: "
                        + this.AcknowledgementMode);
            }

            this.TransactionContext.Commit();
        }

        public void Rollback()
        {
            if(!Transacted)
            {
                throw new InvalidOperationException(
                        "You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: "
                        + this.AcknowledgementMode);
            }

            this.TransactionContext.Rollback();
        }

        public void Recover()
        {
            CheckClosed();

            if (acknowledgementMode == AcknowledgementMode.Transactional)
            {
                throw new IllegalStateException("Cannot Recover a Transacted Session");
            }

            lock(this.consumers.SyncRoot)
            {
                foreach(MessageConsumer consumer in this.consumers.Values)
                {
                    consumer.Rollback();
                }
            }
        }

        #endregion

		public void AddConsumer(MessageConsumer consumer)
		{
			if(!this.closing)
			{
				// Registered with Connection before we register at the broker.
				consumers[consumer.ConsumerId] = consumer;
				connection.addDispatcher(consumer.ConsumerId, this);
			}
		}

		public void RemoveConsumer(MessageConsumer consumer)
		{
			connection.removeDispatcher(consumer.ConsumerId);
			if(!this.closing)
			{
				consumers.Remove(consumer.ConsumerId);
			}
		}

		public void DoSend(Message message, MessageProducer producer, TimeSpan sendTimeout)
        {
            Message msg = message;

            if(Transacted)
            {
                DoStartTransaction();
                msg.TransactionId = TransactionContext.TransactionId;
            }

            msg.RedeliveryCounter = 0;

            if(this.connection.CopyMessageOnSend)
            {
                msg = (Message)msg.Clone();
            }

            msg.OnSend();
            msg.ProducerId = msg.MessageId.ProducerId;

            if(sendTimeout.TotalMilliseconds <= 0 && !msg.ResponseRequired && !connection.AlwaysSyncSend &&
               (!msg.Persistent || connection.AsyncSend || msg.TransactionId != null))
            {
                this.connection.Oneway(msg);
            }
            else
            {
                if(sendTimeout.TotalMilliseconds > 0)
                {
                    this.connection.SyncRequest(msg, sendTimeout);
                }
                else
                {
                    this.connection.SyncRequest(msg);
                }
            }
        }

        /// <summary>
        /// Ensures that a transaction is started
        /// </summary>
        public void DoStartTransaction()
        {
            if(Transacted)
            {
                this.TransactionContext.Begin();
            }
        }

        public void DisposeOf(ConsumerId objectId)
        {
            connection.removeDispatcher(objectId);
            if(!this.closing)
            {
                consumers.Remove(objectId);
            }
        }

        public void DisposeOf(ProducerId objectId)
        {
            if(!this.closing)
            {
                producers.Remove(objectId);
            }
        }

        protected virtual ProducerInfo CreateProducerInfo(IDestination destination)
        {
            ProducerInfo answer = new ProducerInfo();
            answer.ProducerId = GetNextProducerId();
            answer.Destination = Destination.Transform(destination);

            // If the destination contained a URI query, then use it to set public
            // properties on the ProducerInfo
            Destination amqDestination = destination as Destination;
            if(amqDestination != null && amqDestination.Options != null)
            {
                StringDictionary options = URISupport.GetProperties(amqDestination.Options, "producer.");
                URISupport.SetProperties(answer, options);
            }

            return answer;
        }

		public ConsumerId GetNextConsumerId()
		{
			ConsumerId id = new ConsumerId();
			id.ConnectionId = info.SessionId.ConnectionId;
			id.SessionId = info.SessionId.Value;
			id.Value = Interlocked.Increment(ref consumerCounter);

			return id;
		}

		public ProducerId GetNextProducerId()
		{
			ProducerId id = new ProducerId();
			id.ConnectionId = info.SessionId.ConnectionId;
			id.SessionId = info.SessionId.Value;
			id.Value = Interlocked.Increment(ref producerCounter);

			return id;
		}

		public void Stop()
        {
            if(this.executor != null)
            {
                this.executor.Stop();
            }
        }

        public void Start()
        {
            foreach(MessageConsumer consumer in this.consumers.Values)
            {
                consumer.Start();
            }

            if(this.executor != null)
            {
                this.executor.Start();
            }
        }

        public bool Started
        {
            get
            {
                return this.executor != null ? this.executor.Running : false;
            }
        }

        internal void Redispatch(MessageDispatchChannel channel)
        {
            MessageDispatch[] messages = channel.RemoveAll();
            System.Array.Reverse(messages);

            foreach(MessageDispatch message in messages)
            {
                if(Tracer.IsDebugEnabled)
                {
                    Tracer.DebugFormat("Resending Message Dispatch: ", message.ToString());
                }
                this.executor.ExecuteFirst(message);
            }
        }

        public void Dispatch(MessageDispatch dispatch)
        {
            if(this.executor != null)
            {
                if(Tracer.IsDebugEnabled)
                {
                    Tracer.DebugFormat("Send Message Dispatch: ", dispatch.ToString());
                }
                this.executor.Execute(dispatch);
            }
        }

        internal void ClearMessagesInProgress()
        {
            if( this.executor != null ) {
                this.executor.ClearMessagesInProgress();
            }

            if(Transacted)
            {
                this.transactionContext.ResetTransactionInProgress();
            }

            lock(this.consumers.SyncRoot)
            {
                foreach(MessageConsumer consumer in this.consumers.Values)
                {
                    consumer.InProgressClearRequired();
                    ThreadPool.QueueUserWorkItem(ClearMessages, consumer);
                }
            }
        }

        private void ClearMessages(object value)
        {
            MessageConsumer consumer = value as MessageConsumer;

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("Performing Async Clear of In Progress Messages for Consumer: " + consumer.ConsumerId);
            }

            consumer.ClearMessagesInProgress();
        }

        internal void Acknowledge()
        {
            lock(this.consumers.SyncRoot)
            {
                foreach(MessageConsumer consumer in this.consumers.Values)
                {
                    consumer.Acknowledge();
                }
            }
        }

        private Message ConfigureMessage(Message message)
        {
            message.Connection = this.connection;

            if(this.IsTransacted)
            {
                // Allows Acknowledge to be called in a transaction with no effect per JMS Spec.
                message.Acknowledger += new AcknowledgeHandler(DoNothingAcknowledge);
            }

            return message;
        }

        internal void SendAck(MessageAck ack)
        {
            this.SendAck(ack, false);
        }

        internal void SendAck(MessageAck ack, bool lazy)
        {
            if(lazy || connection.SendAcksAsync || this.IsTransacted )
            {
                this.connection.Oneway(ack);
            }
            else
            {
                this.connection.SyncRequest(ack);
            }
        }

        private void CheckClosed()
        {
            if(closed)
            {
                throw new IllegalStateException("The Session is Closed");
            }
        }

        /// <summary>
        /// Prevents message from throwing an exception if a client calls Acknoweldge on
        /// a message that is part of a transaction either being produced or consumed.  The
        /// JMS Spec indicates that users should be able to call Acknowledge with no effect
        /// if the message is in a transaction.
        /// </summary>
        /// <param name="message">
        /// A <see cref="Message"/>
        /// </param>
        private void DoNothingAcknowledge(Message message)
        {
        }

    }
}
