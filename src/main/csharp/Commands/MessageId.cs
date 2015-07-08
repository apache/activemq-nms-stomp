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

namespace Apache.NMS.Stomp.Commands
{
    public class MessageId : BaseDataStructure
    {
        private ProducerId producerId;
        private long producerSequenceId;
        private long brokerSequenceId;
        private string key = null;

        public MessageId() : base()
        {
        }

        public MessageId(ProducerId prodId, long producerSeqId) : base()
        {
            this.producerId = prodId;
            this.producerSequenceId = producerSeqId;
        }

        public MessageId(string value) : base()
        {
            this.SetValue(value);
        }

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.MessageIdType;
        }

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            if(null == this.key)
            {
                this.key = string.Format("{0}:{1}", this.producerId.ToString(), this.producerSequenceId);
            }

            return this.key;
        }

        /// <summary>
        /// Sets the value as a String
        /// </summary>
        public void SetValue(string messageKey)
        {
            string mkey = messageKey;

            this.key = mkey;

            // Parse off the sequenceId
            int p = mkey.LastIndexOf(":");
            if(p >= 0)
            {
                if(Int64.TryParse(mkey.Substring(p + 1), out this.producerSequenceId))
                {
                    mkey = mkey.Substring(0, p);
                }
                else
                {
                    this.producerSequenceId = 0;
                }
            }

            producerId = new ProducerId(mkey);
        }

        public ProducerId ProducerId
        {
            get { return this.producerId; }
            set { this.producerId = value; }
        }

        public long ProducerSequenceId
        {
            get { return this.producerSequenceId; }
            set { this.producerSequenceId = value; }
        }

        public long BrokerSequenceId
        {
            get { return this.brokerSequenceId; }
            set { this.brokerSequenceId = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + HashCode(this.ProducerId);
            answer = (answer * 37) + HashCode(this.ProducerSequenceId);
            answer = (answer * 37) + HashCode(this.BrokerSequenceId);

            return answer;
        }

        public override bool Equals(object that)
        {
            if(that is MessageId)
            {
                return Equals((MessageId) that);
            }

            return false;
        }

        public virtual bool Equals(MessageId that)
        {
            return (Equals(this.ProducerId, that.ProducerId)
                    && Equals(this.ProducerSequenceId, that.ProducerSequenceId)
                    && Equals(this.BrokerSequenceId, that.BrokerSequenceId));
        }
    }
}