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

namespace Apache.NMS.Stomp.Commands
{
    public class TransactionId : BaseDataStructure
    {
        long value;
        ConnectionId connectionId;

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.TransactionIdType;
        }

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            return ConnectionId + ":" + Value;
        }

        public long Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public ConnectionId ConnectionId
        {
            get { return connectionId; }
            set { this.connectionId = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + HashCode(Value);
            answer = (answer * 37) + HashCode(ConnectionId);

            return answer;
        }

        public override bool Equals(object that)
        {
            if(that is TransactionId)
            {
                return Equals((TransactionId) that);
            }
            return false;
        }

        public virtual bool Equals(TransactionId that)
        {
            if(!Equals(this.Value, that.Value))
            {
                return false;
            }
            if(!Equals(this.ConnectionId, that.ConnectionId))
            {
                return false;
            }

            return true;
        }
    };
}

