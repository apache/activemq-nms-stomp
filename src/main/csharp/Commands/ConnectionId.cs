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
    public class ConnectionId : BaseDataStructure
    {
        string value;

        public ConnectionId()
        {
        }

        public ConnectionId(SessionId sessionId)
        {
            this.value = sessionId.ConnectionId;
        }

        public ConnectionId(ProducerId producerId)
        {
            this.value = producerId.ConnectionId;
        }

        public ConnectionId(ConsumerId consumerId)
        {
            this.value = consumerId.ConnectionId;
        }

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.ConnectionIdType;
        }

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            return Value;
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + HashCode(Value);

            return answer;
        }

        public override bool Equals(object that)
        {
            if(that is ConnectionId)
            {
                return Equals((ConnectionId) that);
            }
            return false;
        }

        public virtual bool Equals(ConnectionId that)
        {
            return Equals(this.Value, that.Value);
        }
    };
}

