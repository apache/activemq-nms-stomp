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
    public class ConnectionInfo : BaseCommand
    {
        ConnectionId connectionId;
        string host;
        string clientId;
        string password;
        string userName;

        /// <summery>
        /// Get the unique identifier that this object and its own
        /// Marshaler share.
        /// </summery>
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.ConnectionInfoType;
        }

        /// <summery>
        /// Returns a string containing the information for this DataStructure
        /// such as its type and value of its elements.
        /// </summery>
        public override string ToString()
        {
            return GetType().Name + "[" +
                "ConnectionId=" + ConnectionId + ", " +
                "Host=" + Host + ", " +
                "ClientId=" + ClientId + ", " +
                "Password=" + Password + ", " +
                "UserName=" + UserName +
                "]";
        }

        public ConnectionId ConnectionId
        {
            get { return connectionId; }
            set { this.connectionId = value; }
        }

        public string Host
        {
            get { return host; }
            set { this.host = value; }
        }

        public string ClientId
        {
            get { return clientId; }
            set { this.clientId = value; }
        }

        public string Password
        {
            get { return password; }
            set { this.password = value; }
        }

        public string UserName
        {
            get { return userName; }
            set { this.userName = value; }
        }

        /// <summery>
        /// Return an answer of true to the isConnectionInfo() query.
        /// </summery>
        public override bool IsConnectionInfo
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            return visitor.processAddConnection( this );
        }

    };
}

