// /*
//  * Licensed to the Apache Software Foundation (ASF) under one or more
//  * contributor license agreements.  See the NOTICE file distributed with
//  * this work for additional information regarding copyright ownership.
//  * The ASF licenses this file to You under the Apache License, Version 2.0
//  * (the "License"); you may not use this file except in compliance with
//  * the License.  You may obtain a copy of the License at
//  *
//  *     http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  */
// 
using System;
using Apache.NMS.Util;
using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp
{
    public class StompMessageTransformation : MessageTransformation
    {
        private readonly Connection connection;

        public StompMessageTransformation(Connection connection) : base()
        {
            this.connection = connection;
        }

        #region Creation Methods and Conversion Support Methods

        protected override IMessage DoCreateMessage()
        {
            Message message = new Message();
            message.Connection = this.connection;
            return message;
        }

        protected override IBytesMessage DoCreateBytesMessage()
        {
            BytesMessage message = new BytesMessage();
            message.Connection = this.connection;
            return message;
        }

        protected override ITextMessage DoCreateTextMessage()
        {
            TextMessage message = new TextMessage();
            message.Connection = this.connection;
            return message;
        }

        protected override IStreamMessage DoCreateStreamMessage()
        {
            throw new NotSupportedException("Stomp Cannot process Stream Messages");
        }

        protected override IMapMessage DoCreateMapMessage()
        {
            MapMessage message = new MapMessage();
            message.Connection = this.connection;
            return message;
        }

        protected override IObjectMessage DoCreateObjectMessage()
        {
            throw new NotSupportedException("Stomp Cannot process Object Messages");
        }

        protected override IDestination DoTransformDestination(IDestination destination)
        {
            return Destination.Transform(destination);
        }

        protected override void DoPostProcessMessage(IMessage message)
        {
        }

        #endregion
    }
}

