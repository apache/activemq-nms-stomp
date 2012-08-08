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
using System.Threading;
using Apache.NMS.Policies;
using Apache.NMS.Test;
using NUnit.Framework;

namespace Apache.NMS.Stomp.Test
{
    [TestFixture]
    public class SpecialCharactersTest : NMSTestSupport
    {
        private Connection connection;
        private ISession session;

        [SetUp]
        public override void SetUp()
        {
            this.connection = (Connection) CreateConnection();
        }

        [TearDown]
        public override void TearDown()
        {
            this.session = null;

            if(this.connection != null)
            {
                this.connection.Close();
                this.connection = null;
            }

            base.TearDown();
        }
        
        [Test]
        public void TestSpecialCharacters()
        {
            string message = "Special characters: â è ô ü ö ó ñ";
            connection.Start();

            this.session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
            ITemporaryQueue queue = session.CreateTemporaryQueue();
            IMessageProducer producer = session.CreateProducer(queue);
            IMessageConsumer consumer = session.CreateConsumer(queue);

            //
            // send the message
            IMessage textMessage = session.CreateTextMessage(message);
            producer.Send(textMessage);

            //
            // wait for the response
            ITextMessage messageReceive = (ITextMessage) consumer.Receive(TimeSpan.FromMilliseconds(10000));
            Assert.AreEqual(message, messageReceive.Text);

            session.Close();
        }
    }
}
