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
using System.Collections;

namespace Apache.NMS.Stomp.Commands
{
    public class MessageId : BaseDataStructure
    {
        ProducerId producerId;
        long producerSequenceId;
        long brokerSequenceId;

        private string key = null;

        public MessageId() : base()
        {
        }

        public MessageId(ProducerId producerId, long producerSequenceId) : base()
        {
            this.producerId = producerId;
            this.producerSequenceId = producerSequenceId;
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
            if( key == null )
            {
                key = producerId.ToString() + ":" + producerSequenceId;
            }

            return key;
        }

        /// <summary>
        /// Sets the value as a String
        /// </summary>
        public void SetValue(string messageKey)
        {
            this.key = messageKey;

            // Parse off the sequenceId
            int p = messageKey.LastIndexOf(":");
            if(p >= 0)
            {
                producerSequenceId = Int64.Parse(messageKey.Substring(p + 1));
                messageKey = messageKey.Substring(0, p);
            }
            producerId = new ProducerId(messageKey);
        }

        public ProducerId ProducerId
        {
            get { return producerId; }
            set { this.producerId = value; }
        }

        public long ProducerSequenceId
        {
            get { return producerSequenceId; }
            set { this.producerSequenceId = value; }
        }

        public long BrokerSequenceId
        {
            get { return brokerSequenceId; }
            set { this.brokerSequenceId = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + HashCode(ProducerId);
            answer = (answer * 37) + HashCode(ProducerSequenceId);
            answer = (answer * 37) + HashCode(BrokerSequenceId);

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
            if(!Equals(this.ProducerId, that.ProducerId))
            {
                return false;
            }
            if(!Equals(this.ProducerSequenceId, that.ProducerSequenceId))
            {
                return false;
            }
            if(!Equals(this.BrokerSequenceId, that.BrokerSequenceId))
            {
                return false;
            }

            return true;
        }
    };
}

