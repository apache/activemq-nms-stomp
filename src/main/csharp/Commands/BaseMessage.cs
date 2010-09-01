/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Apache.NMS.Util;
using Apache.NMS.Stomp.State;

namespace Apache.NMS.Stomp.Commands
{
    public class BaseMessage : BaseCommand, MarshallAware, ICloneable
    {
        ProducerId producerId;
        Destination destination;
        TransactionId transactionId;
        MessageId messageId;
        TransactionId originalTransactionId;
        string groupID;
        int groupSequence;
        string correlationId;
        bool persistent;
        long expiration;
        byte priority;
        Destination replyTo;
        long timestamp;
        string type;
        byte[] content;
        byte[] marshalledProperties;
        ConsumerId targetConsumerId;
        int redeliveryCounter;

        private bool readOnlyMsgProperties;
        private bool readOnlyMsgBody;

        public const int DEFAULT_MINIMUM_MESSAGE_SIZE = 1024;

        ///
        /// <summery>
        ///  Clone this object and return a new instance that the caller now owns.
        /// </summery>
        ///
        public override Object Clone()
        {
            // Since we are a derived class use the base's Clone()
            // to perform the shallow copy. Since it is shallow it
            // will include our derived class. Since we are derived,
            // this method is an override.
            Message o = (Message) base.Clone();

            if(this.messageId != null)
            {
                o.MessageId = (MessageId) this.messageId.Clone();
            }

            return o;
        }

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            return GetType().Name + "[" +
                "ProducerId=" + ProducerId + ", " +
                "Destination=" + Destination + ", " +
                "TransactionId=" + TransactionId + ", " +
                "MessageId=" + MessageId + ", " +
                "OriginalTransactionId=" + OriginalTransactionId + ", " +
                "GroupID=" + GroupID + ", " +
                "GroupSequence=" + GroupSequence + ", " +
                "CorrelationId=" + CorrelationId + ", " +
                "Persistent=" + Persistent + ", " +
                "Expiration=" + Expiration + ", " +
                "Priority=" + Priority + ", " +
                "ReplyTo=" + ReplyTo + ", " +
                "Timestamp=" + Timestamp + ", " +
                "Type=" + Type + ", " +
                "Content=" + Content + ", " +
                "MarshalledProperties=" + MarshalledProperties + ", " +
                "TargetConsumerId=" + TargetConsumerId + ", " +
                "RedeliveryCounter=" + RedeliveryCounter +
                "]";
        }

        public virtual int Size()
        {
            int size = DEFAULT_MINIMUM_MESSAGE_SIZE;

            if(marshalledProperties != null)
            {
                size += marshalledProperties.Length;
            }
            if(content != null)
            {
                size += content.Length;
            }

            return size;
        }

        public virtual void OnSend()
        {
            this.ReadOnlyProperties = true;
            this.ReadOnlyBody = true;
        }

        public virtual void OnMessageRollback()
        {
            this.redeliveryCounter++;
        }

        public bool IsExpired()
        {
            return this.expiration == 0 ? false : DateTime.UtcNow > DateUtils.ToDateTimeUtc(this.expiration);
        }

        public ProducerId ProducerId
        {
            get { return producerId; }
            set { this.producerId = value; }
        }

        public Destination Destination
        {
            get { return destination; }
            set { this.destination = value; }
        }

        public TransactionId TransactionId
        {
            get { return transactionId; }
            set { this.transactionId = value; }
        }

        public MessageId MessageId
        {
            get { return messageId; }
            set { this.messageId = value; }
        }

        public TransactionId OriginalTransactionId
        {
            get { return originalTransactionId; }
            set { this.originalTransactionId = value; }
        }

        public string GroupID
        {
            get { return groupID; }
            set { this.groupID = value; }
        }

        public int GroupSequence
        {
            get { return groupSequence; }
            set { this.groupSequence = value; }
        }

        public string CorrelationId
        {
            get { return correlationId; }
            set { this.correlationId = value; }
        }

        public bool Persistent
        {
            get { return persistent; }
            set { this.persistent = value; }
        }

        public long Expiration
        {
            get { return expiration; }
            set { this.expiration = value; }
        }

        public byte Priority
        {
            get { return priority; }
            set { this.priority = value; }
        }

        public Destination ReplyTo
        {
            get { return replyTo; }
            set { this.replyTo = value; }
        }

        public long Timestamp
        {
            get { return timestamp; }
            set { this.timestamp = value; }
        }

        public string Type
        {
            get { return type; }
            set { this.type = value; }
        }

        public byte[] Content
        {
            get { return content; }
            set { this.content = value; }
        }

        public byte[] MarshalledProperties
        {
            get { return marshalledProperties; }
            set { this.marshalledProperties = value; }
        }

        public ConsumerId TargetConsumerId
        {
            get { return targetConsumerId; }
            set { this.targetConsumerId = value; }
        }

        public int RedeliveryCounter
        {
            get { return redeliveryCounter; }
            set { this.redeliveryCounter = value; }
        }

        public virtual bool ReadOnlyProperties
        {
            get { return this.readOnlyMsgProperties; }
            set { this.readOnlyMsgProperties = value; }
        }

        public virtual bool ReadOnlyBody
        {
            get { return this.readOnlyMsgBody; }
            set { this.readOnlyMsgBody = value; }
        }

        ///
        /// <summery>
        ///  Return an answer of true to the isMessage() query.
        /// </summery>
        ///
        public override bool IsMessage
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            return visitor.processMessage( this );
        }

    };
}

