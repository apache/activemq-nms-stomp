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
    public class TransactionInfo : BaseCommand
    {
        public const byte BEGIN = 0;
        public const byte COMMIT = 1;
        public const byte ROLLBACK = 2;

        ConnectionId connectionId;
        TransactionId transactionId;
        byte type;

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.TransactionInfoType;
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
                "ConnectionId=" + ConnectionId + ", " +
                "TransactionId=" + TransactionId + ", " +
                "Type=" + Type +
                "]";
        }

        public ConnectionId ConnectionId
        {
            get { return connectionId; }
            set { this.connectionId = value; }
        }

        public TransactionId TransactionId
        {
            get { return transactionId; }
            set { this.transactionId = value; }
        }

        public byte Type
        {
            get { return type; }
            set { this.type = value; }
        }

        ///
        /// <summery>
        ///  Return an answer of true to the isTransactionInfo() query.
        /// </summery>
        ///
        public override bool IsTransactionInfo
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            switch(type)
            {
                case TransactionInfo.BEGIN:
                    return visitor.processBeginTransaction(this);
                case TransactionInfo.COMMIT:
                    return visitor.processCommitTransaction(this);
                case TransactionInfo.ROLLBACK:
                    return visitor.processRollbackTransaction(this);
                default:
                    throw new IOException("Transaction info type unknown: " + type);
            }
        }
    };
}

