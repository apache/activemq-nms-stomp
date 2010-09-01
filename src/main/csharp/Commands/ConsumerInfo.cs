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
    public class ConsumerInfo : BaseCommand
    {
        public const byte ID_CONSUMERINFO = 5;

        ConsumerId consumerId;
        Destination destination;
        AcknowledgementMode ackMode;
        int prefetchSize;
        int maximumPendingMessageLimit;
        bool dispatchAsync;
        string selector;
        string subscriptionName;
        bool noLocal;
        bool exclusive;
        bool retroactive;
        byte priority;
        string transformation;

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return ID_CONSUMERINFO;
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
                "ConsumerId=" + ConsumerId + ", " +
                "Destination=" + Destination + ", " +
                "Ack Mode=" + AckMode + ", " +
                "PrefetchSize=" + PrefetchSize + ", " +
                "MaximumPendingMessageLimit=" + MaximumPendingMessageLimit + ", " +
                "DispatchAsync=" + DispatchAsync + ", " +
                "Selector=" + Selector + ", " +
                "SubscriptionName=" + SubscriptionName + ", " +
                "NoLocal=" + NoLocal + ", " +
                "Exclusive=" + Exclusive + ", " +
                "Retroactive=" + Retroactive + ", " +
                "Priority=" + Priority + ", " +
                "Transformation" + Transformation + 
                "]";
        }

        public ConsumerId ConsumerId
        {
            get { return consumerId; }
            set { this.consumerId = value; }
        }

        public Destination Destination
        {
            get { return destination; }
            set { this.destination = value; }
        }

        public AcknowledgementMode AckMode
        {
            get { return this.ackMode; }
            set { this.ackMode = value; }
        }

        public int PrefetchSize
        {
            get { return prefetchSize; }
            set { this.prefetchSize = value; }
        }

        public int MaximumPendingMessageLimit
        {
            get { return maximumPendingMessageLimit; }
            set { this.maximumPendingMessageLimit = value; }
        }

        public bool DispatchAsync
        {
            get { return dispatchAsync; }
            set { this.dispatchAsync = value; }
        }

        public string Selector
        {
            get { return selector; }
            set { this.selector = value; }
        }

        public string SubscriptionName
        {
            get { return subscriptionName; }
            set { this.subscriptionName = value; }
        }

        public bool NoLocal
        {
            get { return noLocal; }
            set { this.noLocal = value; }
        }

        public bool Exclusive
        {
            get { return exclusive; }
            set { this.exclusive = value; }
        }

        public bool Retroactive
        {
            get { return retroactive; }
            set { this.retroactive = value; }
        }

        public byte Priority
        {
            get { return priority; }
            set { this.priority = value; }
        }

        public string Transformation
        {
            get { return this.transformation; }
            set { this.transformation = value; }
        }

        ///
        /// <summery>
        ///  Return an answer of true to the isConsumerInfo() query.
        /// </summery>
        ///
        public override bool IsConsumerInfo
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            return visitor.processAddConsumer( this );
        }

    };
}

