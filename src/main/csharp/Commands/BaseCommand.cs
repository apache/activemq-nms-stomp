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
using Apache.NMS.Stomp.State;

namespace Apache.NMS.Stomp.Commands
{
    public abstract class BaseCommand : BaseDataStructure, Command, ICloneable
    {
        private int commandId;
        private bool responseRequired = false;

        public int CommandId
        {
            get { return commandId; }
            set { this.commandId = value; }
        }

        public override int GetHashCode()
        {
            return (CommandId * 37) + GetDataStructureType();
        }

        public override bool Equals(Object that)
        {
            if(that is BaseCommand)
            {
                BaseCommand thatCommand = (BaseCommand) that;
                return this.GetDataStructureType() == thatCommand.GetDataStructureType()
                    && this.CommandId == thatCommand.CommandId;
            }
            return false;
        }

        public override String ToString()
        {
            string answer = DataStructureTypes.GetDataStructureTypeAsString(GetDataStructureType());
            if(answer.Length == 0)
            {
                answer = base.ToString();
            }
            return answer + ": id = " + CommandId;
        }

        public virtual bool IsBrokerInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsConnectionInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsConnectionError
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsConsumerInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsControlCommand
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsDestinationInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsMessage
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsMessageAck
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsMessageDispatch
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsProducerAck
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsProducerInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsRemoveInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsRemoveSubscriptionInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsResponse
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsSessionInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsShutdownInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsTransactionInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsErrorCommand
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsKeepAliveInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsWireFormatInfo
        {
            get
            {
                return false;
            }
        }

        public virtual bool ResponseRequired
        {
            get
            {
                return responseRequired;
            }
            set
            {
                responseRequired = value;
            }
        }

        public virtual Response visit(ICommandVisitor visitor)
        {
            throw new ApplicationException("BaseCommand.Visit() not implemented");
        }

        public override Object Clone()
        {
            // Since we are a derived class use the base's Clone()
            // to perform the shallow copy. Since it is shallow it
            // will include our derived class. Since we are derived,
            // this method is an override.
            BaseCommand o = (BaseCommand) base.Clone();

            return o;
        }
    }
}

