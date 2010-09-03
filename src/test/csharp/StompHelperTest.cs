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
using Apache.NMS.Stomp.Commands;
using Apache.NMS.Stomp.Protocol;
using NUnit.Framework;

namespace Apache.NMS.Stomp.Test
{
    [TestFixture]
    public class StompHelperTest
    {
        [Test]
        public void ConsumerIdMarshallingWorks()
        {
            ConsumerId id = new ConsumerId();
            id.ConnectionId = "cheese";
            id.SessionId = 2;
            id.Value = 3;

            string text = id.ToString();
            Assert.AreEqual("cheese:2:3", text, "ConsumerId as stomp");

            ConsumerId another = new ConsumerId("abc:5:6");
            Assert.AreEqual("abc", another.ConnectionId, "extracting consumerId.ConnectionId");
            Assert.AreEqual(5, another.SessionId, "extracting consumerId.SessionId");
            Assert.AreEqual(6, another.Value, "extracting consumerId.Value");
        }

        [Test]
        public void MessageIdMarshallingWorks()
        {
            ProducerId id = new ProducerId();
            id.ConnectionId = "cheese";
            id.SessionId = 2;
            id.Value = 3;

            MessageId mid = new MessageId();
            mid.ProducerId = id;
            mid.BrokerSequenceId = 5;
            mid.ProducerSequenceId = 6;

            string text = mid.ToString();
            Assert.AreEqual("cheese:2:3:6", text, "MessageId as stomp");

            MessageId mid2 = new MessageId("abc:5:6:7:8");
            Assert.AreEqual(8, mid2.ProducerSequenceId, "extracting mid2.ProducerSequenceId");

            ProducerId another = mid2.ProducerId;
            Assert.AreEqual(7, another.Value, "extracting another.Value");
            Assert.AreEqual(6, another.SessionId, "extracting another.SessionId");
            Assert.AreEqual("abc:5", another.ConnectionId, "extracting another.ConnectionId");
        }

        // TODO destination stuff

//        [Test]
//        public void DestinationMarshallingWorks()
//        {
//            Assert.AreEqual("/queue/FOO.BAR", StompHelper.ToStomp(new Queue("FOO.BAR")), "queue");
//            Assert.AreEqual("/topic/FOO.BAR", StompHelper.ToStomp(new Topic("FOO.BAR")), "topic");
//            Assert.AreEqual("/temp-queue/FOO.BAR", StompHelper.ToStomp(new TempQueue("FOO.BAR")),
//                            "temporary queue");
//            Assert.AreEqual("/temp-topic/FOO.BAR", StompHelper.ToStomp(new TempTopic("FOO.BAR")),
//                            "temporary topic");
//
//            Assert.AreEqual(new Queue("FOO.BAR"), StompHelper.ToDestination("/queue/FOO.BAR"),
//                            "queue from Stomp");
//            Assert.AreEqual(new Topic("FOO.BAR"), StompHelper.ToDestination("/topic/FOO.BAR"),
//                            "topic from Stomp");
//            Assert.AreEqual(new TempQueue("FOO.BAR"),
//                            StompHelper.ToDestination("/temp-queue/FOO.BAR"), "temporary queue from Stomp");
//            Assert.AreEqual(new TempTopic("FOO.BAR"),
//                            StompHelper.ToDestination("/temp-topic/FOO.BAR"), "temporary topic from Stomp");
//        }
    }
}
