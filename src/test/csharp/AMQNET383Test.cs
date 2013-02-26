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

/*
 * The main goal of this test list is to make sure that a producer and a consumer with
 * separate connections can communicate properly and consitently despite a waiting time
 * between messages TestStability with 500 sleep would be the important test, as a delay of
 * 30s is executed at each 100 (100, 200, 300, ...).
 */

using System;
using System.Threading;
using Apache.NMS.Test;
using NUnit.Framework;

namespace Apache.NMS.Stomp.Test
{
    [TestFixture]
    public class NMSTestStability : NMSTestSupport
    {
        // TODO set proper configuration parameters
        private const string destination = "TEST.Stability";

        private static int numberOfMessages = 0;
        private static IConnection producerConnection = null;
        private static IConnection consumerConnection = null;
        private static Thread consumerThread = null;
        private static Thread producerThread = null;
        private static long consumerMessageCounter = 0;
        private static long producerMessageCounter = 0;
        private static string possibleConsumerException = "";
        private static string possibleProducerException = "";
        private static bool consumerReady = false;

        [SetUp]
        public void Init()
        {
            if (producerConnection != null)
            {
                producerConnection.Close();
            }
            if (consumerConnection != null)
            {
                consumerConnection.Close();
            }
            if (consumerThread != null)
            {
                consumerThread.Abort();
            }
            if (producerThread != null)
            {
                producerThread.Abort();
            }

            producerConnection = null;
            consumerConnection = null;
            consumerThread = null;
            producerThread = null;

            producerConnection = CreateConnection();
            consumerConnection = CreateConnection();

            numberOfMessages = 0;
            consumerMessageCounter = 0;
            producerMessageCounter = 0;
            possibleConsumerException = "";
            possibleProducerException = "";
            consumerReady = false;
            //Giving time for the topic to clear out
            Thread.Sleep(2500);
        }

        [TearDown]
        public void Dispose()
        {
            if (producerConnection != null)
            {
                producerConnection.Close();
            }
            if (consumerConnection != null)
            {
                consumerConnection.Close();
            }

            if (consumerThread != null)
            {
                consumerThread.Abort();
            }
            if (producerThread != null)
            {
                producerThread.Abort();
            }

            producerConnection = null;
            consumerConnection = null;
            consumerThread = null;
            producerThread = null;
        }

#if !NETCF
		public enum ProducerTestType
		{
			CONTINUOUS,
			WITHSLEEP
		}

        [Test]
        public void TestStability(
            [Values(5, 50, 500)]
            int testMessages,
			[Values(ProducerTestType.CONTINUOUS, ProducerTestType.WITHSLEEP)]
            ProducerTestType producerType)
        {
            //At 100,200,300, ... a delay of 30 seconds is executed in the producer to cause an unexpected disconnect, due to a malformed ACK?
            numberOfMessages = testMessages;

            consumerThread = new Thread(NMSTestStability.ConsumerThread);
            consumerThread.Start();

			if(ProducerTestType.CONTINUOUS == producerType)
			{
				producerThread = new Thread(NMSTestStability.ProducerThreadContinuous);
			}
			else
			{
				producerThread = new Thread(NMSTestStability.ProducerThreadWithSleep);
			}
			
            producerThread.Start();

            Thread.Sleep(100);

            Assert.IsTrue(consumerThread.IsAlive && producerThread.IsAlive);

            while (consumerThread.IsAlive && producerThread.IsAlive)
            {
                Thread.Sleep(100);
            }

            Assert.IsEmpty(possibleConsumerException);
            Assert.IsEmpty(possibleProducerException);
            Assert.AreEqual(numberOfMessages, producerMessageCounter);
            Assert.AreEqual(numberOfMessages, consumerMessageCounter);
        }
#endif

        #region Consumer
        private static void ConsumerThread()
        {
            ISession session = consumerConnection.CreateSession();
            IMessageConsumer consumer;
            IDestination dest = session.GetTopic(destination);
            consumer = session.CreateConsumer(dest);
            consumer.Listener += new MessageListener(OnMessage);
            consumerConnection.ExceptionListener += new ExceptionListener(OnConsumerExceptionListener);
            consumerConnection.Start();

            consumerReady = true;

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private static void OnMessage(IMessage receivedMsg)
        {
            consumerMessageCounter++;
        }

        public static void OnConsumerExceptionListener(Exception ex)
        {
            possibleConsumerException = ex.Message;
            Thread.CurrentThread.Abort();
        }
        #endregion Consumer

        #region Producer
        private static void ProducerThreadWithSleep()
        {
            ISession session = producerConnection.CreateSession();
            IMessageProducer producer;

            IDestination dest = session.GetTopic(destination);
            producer = session.CreateProducer(dest);
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            producerConnection.ExceptionListener += new ExceptionListener(OnProducerExceptionListener);
            producerConnection.Start();

            ITextMessage message;

            while (!consumerReady)
            {
                Thread.Sleep(100);
            }

            for (int c = 0; c < numberOfMessages; c++)
            {
                message = session.CreateTextMessage(c.ToString());
                message.NMSType = "testType";
                producer.Send(message);
                producerMessageCounter++;
                //Focal point of this test; induce a "long" delay between two messages without any other communication on the topic, Note that thse delays occure only at each 100, and that messages can be sent after the delay before a possible disconnect
                if ((c + 1) % 100 == 0)
                {
                    Thread.Sleep(30000);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private static void ProducerThreadContinuous()
        {
            ISession session = producerConnection.CreateSession();
            IMessageProducer producer;

            IDestination dest = session.GetTopic(destination);
            producer = session.CreateProducer(dest);
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            producerConnection.ExceptionListener += new ExceptionListener(OnProducerExceptionListener);
            producerConnection.Start();

            ITextMessage message;

            while (!consumerReady)
            {
                Thread.Sleep(100);
            }

            for (int c = 0; c < numberOfMessages; c++)
            {
                message = session.CreateTextMessage(c.ToString());
                message.NMSType = "testType";
                producer.Send(message);
                producerMessageCounter++;
                Thread.Sleep(10);
            }
        }

        public static void OnProducerExceptionListener(Exception ex)
        {
            possibleProducerException = ex.Message;
            Thread.CurrentThread.Abort();
        }
        #endregion Producer

    }
}