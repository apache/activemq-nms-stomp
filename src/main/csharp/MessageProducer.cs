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
using Apache.NMS.Util;
using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp
{
    /// <summary>
    /// An object capable of sending messages to some destination
    /// </summary>
    public class MessageProducer : IMessageProducer
    {
        private Session session;
        private bool closed = false;
        private readonly object closedLock = new object();
        private readonly ProducerInfo info;
        private int producerSequenceId = 0;

        private MsgDeliveryMode msgDeliveryMode = NMSConstants.defaultDeliveryMode;
        private TimeSpan requestTimeout;
        private TimeSpan msgTimeToLive = NMSConstants.defaultTimeToLive;
        private MsgPriority msgPriority = NMSConstants.defaultPriority;
        private bool disableMessageID = false;
        private bool disableMessageTimestamp = false;
        protected bool disposed = false;

        private readonly MessageTransformation messageTransformation;

        public MessageProducer(Session session, ProducerInfo info)
        {
            this.session = session;
            this.info = info;
            this.RequestTimeout = session.RequestTimeout;
            this.messageTransformation = session.Connection.MessageTransformation;
        }

        ~MessageProducer()
        {
            Dispose(false);
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
            lock(closedLock)
            {
                if(closed)
                {
                    return;
                }

                DoClose();
                this.session = null;
            }
        }

        internal void DoClose()
        {
            lock(closedLock)
            {
                if(closed)
                {
                    return;
                }

                try
                {
                    session.DisposeOf(info.ProducerId);
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Error during producer close: {0}", ex);
                }

                closed = true;
            }
        }

        public void Send(IMessage message)
        {
            Send(info.Destination, message, this.msgDeliveryMode, this.msgPriority, this.msgTimeToLive);
        }

        public void Send(IDestination destination, IMessage message)
        {
            Send(destination, message, this.msgDeliveryMode, this.msgPriority, this.msgTimeToLive);
        }

        public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            Send(info.Destination, message, deliveryMode, priority, timeToLive);
        }

        public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            if(null == destination)
            {
                // See if this producer was created without a destination.
                if(null == info.Destination)
                {
                    throw new NotSupportedException();
                }

                // The producer was created with a destination, but an invalid destination
                // was specified.
                throw new Apache.NMS.InvalidDestinationException();
            }

            Destination dest = null;

            if(destination == this.info.Destination)
            {
                dest = destination as Destination;
            }
            else if(info.Destination == null)
            {
                dest = Destination.Transform(destination);
            }
            else
            {
                throw new NotSupportedException("This producer can only send messages to: " + this.info.Destination.PhysicalName);
            }

            if(this.producerTransformer != null)
            {
                IMessage transformed = this.producerTransformer(this.session, this, message);
                if(transformed != null)
                {
                    message = transformed;
                }
            }

            Message stompMessage = this.messageTransformation.TransformMessage<Message>(message);

            stompMessage.ProducerId = info.ProducerId;
            stompMessage.FromDestination = dest;
            stompMessage.NMSDeliveryMode = deliveryMode;
            stompMessage.NMSPriority = priority;

            // Always set the message Id regardless of the disable flag.
            MessageId id = new MessageId();
            id.ProducerId = info.ProducerId;
            id.ProducerSequenceId = Interlocked.Increment(ref this.producerSequenceId);
            stompMessage.MessageId = id;

            if(!disableMessageTimestamp)
            {
                stompMessage.NMSTimestamp = DateTime.UtcNow;
            }

            if(timeToLive != TimeSpan.Zero)
            {
                stompMessage.NMSTimeToLive = timeToLive;
            }

            lock(closedLock)
            {
                if(closed)
                {
                    throw new ConnectionClosedException();
                }
                session.DoSend(stompMessage, this, this.RequestTimeout);
            }
        }

        public ProducerId ProducerId
        {
            get { return info.ProducerId; }
        }

        public MsgDeliveryMode DeliveryMode
        {
            get { return msgDeliveryMode; }
            set { this.msgDeliveryMode = value; }
        }

        public TimeSpan TimeToLive
        {
            get { return msgTimeToLive; }
            set { this.msgTimeToLive = value; }
        }

        public TimeSpan RequestTimeout
        {
            get { return requestTimeout; }
            set { this.requestTimeout = value; }
        }

        public MsgPriority Priority
        {
            get { return msgPriority; }
            set { this.msgPriority = value; }
        }

        public bool DisableMessageID
        {
            get { return disableMessageID; }
            set { this.disableMessageID = value; }
        }

        public bool DisableMessageTimestamp
        {
            get { return disableMessageTimestamp; }
            set { this.disableMessageTimestamp = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.producerTransformer; }
            set { this.producerTransformer = value; }
        }

        #region Message Creation Factory Methods.

        public IMessage CreateMessage()
        {
            return session.CreateMessage();
        }

        public ITextMessage CreateTextMessage()
        {
            return session.CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            return session.CreateTextMessage(text);
        }

        public IMapMessage CreateMapMessage()
        {
            return session.CreateMapMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            throw new NotSupportedException("No Object Message in Stomp");
        }

        public IBytesMessage CreateBytesMessage()
        {
            return session.CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            return session.CreateBytesMessage(body);
        }

        public IStreamMessage CreateStreamMessage()
        {
            return session.CreateStreamMessage();
        }

        #endregion

    }
}
