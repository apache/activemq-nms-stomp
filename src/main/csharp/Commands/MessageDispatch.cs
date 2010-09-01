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

using Apache.NMS.Stomp.State;

namespace Apache.NMS.Stomp.Commands
{
    public class MessageDispatch : BaseCommand
    {
        ConsumerId consumerId;
        Destination destination;
        Message message;
        int redeliveryCounter;

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.MessageDispatchType;
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
                "ConsumerId=" + ConsumerId + ", " +
                "Destination=" + Destination + ", " +
                "Message=" + Message + ", " +
                "RedeliveryCounter=" + RedeliveryCounter + 
                "]";
        }

        public ConsumerId ConsumerId
        {
            get { return consumerId; }
            set { this.consumerId = value; }
        }

        public Destination Destination
        {
            get { return destination; }
            set { this.destination = value; }
        }

        public Message Message
        {
            get { return message; }
            set { this.message = value; }
        }

        public int RedeliveryCounter
        {
            get { return redeliveryCounter; }
            set { this.redeliveryCounter = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + HashCode(ConsumerId);
            answer = (answer * 37) + HashCode(Destination);
            answer = (answer * 37) + HashCode(Message);
            answer = (answer * 37) + HashCode(RedeliveryCounter);

            return answer;
        }

        public override bool Equals(object that)
        {
            if(that is MessageDispatch)
            {
                return Equals((MessageDispatch) that);
            }
            return false;
        }

        public virtual bool Equals(MessageDispatch that)
        {
            if(!Equals(this.ConsumerId, that.ConsumerId))
            {
                return false;
            }
            if(!Equals(this.Destination, that.Destination))
            {
                return false;
            }
            if(!Equals(this.Message, that.Message))
            {
                return false;
            }
            if(!Equals(this.RedeliveryCounter, that.RedeliveryCounter))
            {
                return false;
            }

            return true;
        }
        ///
        /// <summery>
        ///  Return an answer of true to the isMessageDispatch() query.
        /// </summery>
        ///
        public override bool IsMessageDispatch
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            return visitor.processMessageDispatch( this );
        }

    };
}

