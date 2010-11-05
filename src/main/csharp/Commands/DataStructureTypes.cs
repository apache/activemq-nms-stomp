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

namespace Apache.NMS.Stomp.Commands
{      
    public class DataStructureTypes
    {
        public const byte ErrorType = 0;
        
        public const byte MessageType = 1;
        public const byte BytesMessageType = 2;
        public const byte MapMessageType = 3;
        public const byte StreamMessageType = 4;
        public const byte TextMessageType = 5;

        public const byte MessageDispatchType = 9;        
        public const byte MessageIdType = 10;
        public const byte MessageAckType = 11;

        public const byte ConnectionInfoType = 12;
        public const byte ConnectionIdType = 13;
        public const byte ConsumerInfoType = 14;
        public const byte ConsumerIdType = 15;
        public const byte ProducerInfoType = 16;
        public const byte ProducerIdType = 17;
        public const byte SessionInfoType = 18;
        public const byte SessionIdType = 19;
        public const byte TransactionInfoType = 20;
        public const byte TransactionIdType = 21;
        public const byte SubscriptionInfoType = 22;
        public const byte ShutdownInfoType = 23;
        public const byte ResponseType = 24;
        public const byte RemoveInfoType = 25;
        public const byte RemoveSubscriptionInfoType = 26;
        public const byte ErrorResponseType = 27;
        public const byte KeepAliveInfoType = 28;
        public const byte WireFormatInfoType = 29;

        public const byte DestinationType = 48;
        public const byte TempDestinationType = 49;
        public const byte TopicType = 50;
        public const byte TempTopicType = 51;
        public const byte QueueType = 52;
        public const byte TempQueueType = 53;

        public static String GetDataStructureTypeAsString(int type)
        {
            String packetTypeStr = "UnknownType";
            switch(type)
            {
            case ErrorType:
                packetTypeStr = "ErrorType";
                break;
            case MessageType:
                packetTypeStr = "MessageType";
                break;
            case BytesMessageType:
                packetTypeStr = "BytesMessageType";
                break;
            case StreamMessageType:
                packetTypeStr = "StreamMessageType";
                break;
            case TextMessageType:
                packetTypeStr = "TextMessageType";
                break;
            case MessageDispatchType:
                packetTypeStr = "MessageDispatchType";
                break;
            case MessageIdType:
                packetTypeStr = "MessageIdType";
                break;
            case MessageAckType:
                packetTypeStr = "MessageAckType";
                break;
            case ConnectionInfoType:
                packetTypeStr = "ConnectionInfoType";
                break;
            case ConnectionIdType:
                packetTypeStr = "ConnectionIdType";
                break;
            case ConsumerInfoType:
                packetTypeStr = "ConsumerInfoType";
                break;
            case ConsumerIdType:
                packetTypeStr = "ConsumerIdType";
                break;
            case ProducerInfoType:
                packetTypeStr = "ProducerInfoType";
                break;
            case ProducerIdType:
                packetTypeStr = "ProducerIdType";
                break;
            case SessionInfoType:
                packetTypeStr = "SessionInfoType";
                break;
            case TransactionInfoType:
                packetTypeStr = "TransactionInfoType";
                break;
            case TransactionIdType:
                packetTypeStr = "TransactionIdType";
                break;
            case SubscriptionInfoType:
                packetTypeStr = "SubscriptionInfoType";
                break;
            case ShutdownInfoType:
                packetTypeStr = "ShutdownInfoType";
                break;
            case ResponseType:
                packetTypeStr = "ResponseType";
                break;
            case RemoveInfoType:
                packetTypeStr = "RemoveInfoType";
                break;
            case ErrorResponseType:
                packetTypeStr = "ErrorResponseType";
                break;
            case KeepAliveInfoType:
                packetTypeStr = "KeepAliveInfoType";
                break;
            case WireFormatInfoType:
                packetTypeStr = "WireFormatInfoType";
                break;
            case DestinationType:
                packetTypeStr = "DestinationType";
                break;
            case TempDestinationType:
                packetTypeStr = "TempDestinationType";
                break;
            case TopicType:
                packetTypeStr = "TopicType";
                break;
            case TempTopicType:
                packetTypeStr = "TempTopicType";
                break;
            case QueueType:
                packetTypeStr = "QueueType";
                break;
            case TempQueueType:
                packetTypeStr = "TempQueueType";
                break;
            }

            return packetTypeStr;
        }
    }
}
