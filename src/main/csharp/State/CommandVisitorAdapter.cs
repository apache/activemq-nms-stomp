/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp.State
{
    public class CommandVisitorAdapter : ICommandVisitor
    {
        public virtual Response processAddConnection(ConnectionInfo info)
        {
            return null;
        }

        public virtual Response processAddConsumer(ConsumerInfo info)
        {
            return null;
        }

        public virtual Response processKeepAliveInfo(KeepAliveInfo info)
        {
            return null;
        }

        public virtual Response processMessage(Message send)
        {
            return null;
        }

        public virtual Response processMessageAck(MessageAck ack)
        {
            return null;
        }

        public virtual Response processRemoveConnection(ConnectionId id)
        {
            return null;
        }

        public virtual Response processRemoveConsumer(ConsumerId id)
        {
            return null;
        }

        public virtual Response processRemoveSubscriptionInfo(RemoveSubscriptionInfo info)
        {
            return null;
        }

        public virtual Response processShutdownInfo(ShutdownInfo info)
        {
            return null;
        }

        public virtual Response processMessageDispatch(MessageDispatch dispatch)
        {
            return null;
        }

        public virtual Response processResponse(Response response)
        {
            return null;
        }

        public virtual Response processConnectionError(ConnectionError error)
        {
            return null;
        }

    }
}
