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
using Apache.NMS.Stomp.Transport;
using Apache.NMS.Util;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace Apache.NMS.Stomp.Protocol
{
    /// <summary>
    /// Implements the <a href="http://stomp.codehaus.org/">STOMP</a> protocol.
    /// </summary>
    public class StompWireFormat : IWireFormat
    {
        private Encoding encoder = new UTF8Encoding();
        private IPrimitiveMapMarshaler mapMarshaler = new XmlPrimitiveMapMarshaler();
        private ITransport transport;
        private WireFormatInfo remoteWireFormatInfo;
        private int connectedResponseId = -1;
        private bool encodeHeaders = false;
        private int maxInactivityDuration = 30000;
        private int maxInactivityDurationInitialDelay = 0;

        public StompWireFormat()
        {
        }

        public ITransport Transport
        {
            get { return transport; }
            set { transport = value; }
        }

        public int Version
        {
            get { return 1; }
        }

        public Encoding Encoder
        {
            get { return this.encoder; }
            set { this.encoder = value; }
        }

        public IPrimitiveMapMarshaler MapMarshaler
        {
            get { return this.mapMarshaler; }
            set { this.mapMarshaler = value; }
        }

        public int MaxInactivityDuration
        {
            get { return this.maxInactivityDuration; }
            set { this.maxInactivityDuration = value; }
        }

        public int MaxInactivityDurationInitialDelay
        {
            get { return this.maxInactivityDurationInitialDelay; }
            set { this.maxInactivityDurationInitialDelay = value; }
        }

        public long ReadCheckInterval
        {
            get { return this.MaxInactivityDuration; }
        }

        public long WriteCheckInterval
        {
            get { return maxInactivityDuration > 3 ? maxInactivityDuration / 3 : maxInactivityDuration; }
        }

        public void Marshal(Object o, BinaryWriter dataOut)
        {
            Tracer.Debug("StompWireFormat - Marshaling: " + o);

            if(o is ConnectionInfo)
            {
                WriteConnectionInfo((ConnectionInfo) o, dataOut);
            }
            else if(o is Message)
            {
                WriteMessage((Message) o, dataOut);
            }
            else if(o is ConsumerInfo)
            {
                WriteConsumerInfo((ConsumerInfo) o, dataOut);
            }
            else if(o is MessageAck)
            {
                WriteMessageAck((MessageAck) o, dataOut);
            }
            else if(o is TransactionInfo)
            {
                WriteTransactionInfo((TransactionInfo) o, dataOut);
            }
            else if(o is ShutdownInfo)
            {
                WriteShutdownInfo((ShutdownInfo) o, dataOut);
            }
            else if(o is RemoveInfo)
            {
                WriteRemoveInfo((RemoveInfo) o, dataOut);
            }
            else if(o is KeepAliveInfo)
            {
                WriteKeepAliveInfo((KeepAliveInfo) o, dataOut);
            }
            else if(o is Command)
            {
                Command command = o as Command;
                if(command.ResponseRequired)
                {
                    Response response = new Response();
                    response.CorrelationId = command.CommandId;
                    SendCommand(response);
                    Tracer.Debug("StompWireFormat - Autorespond to command: " + o.GetType());
                }
            }
            else
            {
                Tracer.Debug("StompWireFormat - Ignored command: " + o.GetType());
            }
        }

        public Object Unmarshal(BinaryReader dataIn)
        {            
            StompFrame frame = new StompFrame(this.encodeHeaders);
            frame.FromStream(dataIn);

            if (Tracer.IsDebugEnabled)
            {
                Tracer.Debug("Unmarshalled frame: " + frame);
            }

            Object answer = CreateCommand(frame);
            return answer;
        }

        protected virtual Object CreateCommand(StompFrame frame)
        {
            string command = frame.Command;

            if (Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Received " + frame.ToString());
            }

            if(command == "RECEIPT")
            {
                string text = frame.RemoveProperty("receipt-id");
                if(text != null)
                {
                    Response answer = new Response();
                    if(text.StartsWith("ignore:"))
                    {
                        text = text.Substring("ignore:".Length);
                    }

                    answer.CorrelationId = Int32.Parse(text);
                    return answer;
                }
            }
            else if(command == "CONNECTED")
            {
                return ReadConnected(frame);
            }
            else if(command == "ERROR")
            {
                string text = frame.RemoveProperty("receipt-id");
                
                if(text != null && text.StartsWith("ignore:"))
                {
                    Response answer = new Response();
                    answer.CorrelationId = Int32.Parse(text.Substring("ignore:".Length));
                    return answer;
                }
                else
                {
                    ExceptionResponse answer = new ExceptionResponse();
                    if(text != null)
                    {
                        answer.CorrelationId = Int32.Parse(text);
                    }

                    BrokerError error = new BrokerError();
                    error.Message = frame.RemoveProperty("message");
                    answer.Exception = error;
                    return answer;
                }
            }
            else if(command == "KEEPALIVE")
            {
                return new KeepAliveInfo();
            }
            else if(command == "MESSAGE")
            {
                return ReadMessage(frame);
            }
            
            Tracer.Error("Unknown command: " + frame.Command + " headers: " + frame.Properties);
            
            return null;
        }

        protected virtual Command ReadConnected(StompFrame frame)
        {
            this.remoteWireFormatInfo = new WireFormatInfo();

            if(frame.HasProperty("version"))
            {
 				remoteWireFormatInfo.Version = Single.Parse(frame.RemoveProperty("version"), 
				                                            CultureInfo.InvariantCulture);
                if(remoteWireFormatInfo.Version > 1.0f)
                {
                    this.encodeHeaders = true;
                }

                if(frame.HasProperty("session"))
                {
                    remoteWireFormatInfo.Session = frame.RemoveProperty("session");
                }

                if(frame.HasProperty("heart-beat"))
                {
                    string[] hearBeats = frame.RemoveProperty("heart-beat").Split(",".ToCharArray());
                    if(hearBeats.Length != 2)
                    {
                        throw new IOException("Malformed heartbeat property in Connected Frame.");
                    }

                    remoteWireFormatInfo.WriteCheckInterval = Int32.Parse(hearBeats[0].Trim());
                    remoteWireFormatInfo.ReadCheckInterval = Int32.Parse(hearBeats[1].Trim());
                }
            }
            else
            {
                remoteWireFormatInfo.ReadCheckInterval = 0;
                remoteWireFormatInfo.WriteCheckInterval = 0;
                remoteWireFormatInfo.Version = 1.0f;
            }

            if(this.connectedResponseId != -1)
            {
                Response answer = new Response();
                answer.CorrelationId = this.connectedResponseId;
                SendCommand(answer);
                this.connectedResponseId = -1;
            }
            else
            {
                throw new IOException("Received Connected Frame without a set Response Id for it.");
            }

            return remoteWireFormatInfo;
        }

        protected virtual Command ReadMessage(StompFrame frame)
        {
            Message message = null;
            string transformation = frame.RemoveProperty("transformation");

            if(frame.HasProperty("content-length"))
            {
                message = new BytesMessage();
                message.Content = frame.Content;
            }
            else if(transformation == "jms-map-xml")
            {
                message = new MapMessage(this.mapMarshaler.Unmarshal(frame.Content) as PrimitiveMap);
            }
            else
            {
                message = new TextMessage(encoder.GetString(frame.Content, 0, frame.Content.Length));
            }

            // Remove any receipt header we might have attached if the outbound command was
            // sent with response required set to true
            frame.RemoveProperty("receipt");

            // Clear any attached content length headers as they aren't needed anymore and can
            // clutter the Message Properties.
            frame.RemoveProperty("content-length");

            message.Type = frame.RemoveProperty("type");
            message.Destination = Destination.ConvertToDestination(frame.RemoveProperty("destination"));
            message.ReplyTo = Destination.ConvertToDestination(frame.RemoveProperty("reply-to"));
            message.TargetConsumerId = new ConsumerId(frame.RemoveProperty("subscription"));
            message.CorrelationId = frame.RemoveProperty("correlation-id");
            message.MessageId = new MessageId(frame.RemoveProperty("message-id"));
            message.Persistent = StompHelper.ToBool(frame.RemoveProperty("persistent"), false);

            // If it came from NMS.Stomp we added this header to ensure its reported on the
            // receiver side.
			if(frame.HasProperty("NMSXDeliveryMode"))
			{
                message.Persistent = StompHelper.ToBool(frame.RemoveProperty("NMSXDeliveryMode"), false);
            }

            if(frame.HasProperty("priority"))
            {
                message.Priority = Byte.Parse(frame.RemoveProperty("priority"));
            }
            
            if(frame.HasProperty("timestamp"))
            {
                message.Timestamp = Int64.Parse(frame.RemoveProperty("timestamp"));
            }
            
            if(frame.HasProperty("expires"))
            {
                message.Expiration = Int64.Parse(frame.RemoveProperty("expires"));
            }

            if(frame.RemoveProperty("redelivered") != null)
            {
                // We aren't told how many times that the message was redelivered so if it
                // is tagged as redelivered we always set the counter to one.
                message.RedeliveryCounter = 1;
            }

            // now lets add the generic headers
            foreach(string key in frame.Properties.Keys)
            {
                Object value = frame.Properties[key];
                if(value != null)
                {
                    // lets coerce some standard header extensions
                    if(key == "JMSXGroupSeq" || key == "NMSXGroupSeq")
                    {
                        value = Int32.Parse(value.ToString());
		                message.Properties["NMSXGroupSeq"] = value;
						continue;
                    }
					else if(key == "JMSXGroupID" || key == "NMSXGroupID")
					{
		                message.Properties["NMSXGroupID"] = value;
						continue;
					}
                }
                message.Properties[key] = value;
            }
            
            MessageDispatch dispatch = new MessageDispatch();
            dispatch.Message = message;
            dispatch.ConsumerId = message.TargetConsumerId;
            dispatch.Destination = message.Destination;
            dispatch.RedeliveryCounter = message.RedeliveryCounter;
            
            return dispatch;
        }

        protected virtual void WriteMessage(Message command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("SEND", encodeHeaders);
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("destination", Destination.ConvertToStompString(command.Destination));
            
            if(command.ReplyTo != null)
            {
                frame.SetProperty("reply-to", Destination.ConvertToStompString(command.ReplyTo));
            }
            if(command.CorrelationId != null )
            {
                frame.SetProperty("correlation-id", command.CorrelationId);
            }
            if(command.Expiration != 0)
            {
                frame.SetProperty("expires", command.Expiration);
            }
            if(command.Timestamp != 0)
            {
                frame.SetProperty("timestamp", command.Timestamp);
            }
            if(command.Priority != 4)
            {                
                frame.SetProperty("priority", command.Priority);
            }
            if(command.Type != null)
            {
                frame.SetProperty("type", command.Type);
            }
            if(command.TransactionId!=null)
            {
                frame.SetProperty("transaction", command.TransactionId.ToString());
            }

            frame.SetProperty("persistent", command.Persistent.ToString().ToLower());
            frame.SetProperty("NMSXDeliveryMode", command.Persistent.ToString().ToLower());

			if(command.NMSXGroupID != null)
			{
				frame.SetProperty("JMSXGroupID", command.NMSXGroupID);
				frame.SetProperty("NMSXGroupID", command.NMSXGroupID);
                frame.SetProperty("JMSXGroupSeq", command.NMSXGroupSeq);
                frame.SetProperty("NMSXGroupSeq", command.NMSXGroupSeq);
			}

            // Perform any Content Marshaling.
            command.BeforeMarshall(this);
            
            // Store the Marshaled Content.
            frame.Content = command.Content;

            if(command is BytesMessage)
            {
                if(command.Content != null && command.Content.Length > 0)
                {
                    frame.SetProperty("content-length", command.Content.Length);
                }

                frame.SetProperty("transformation", "jms-byte");
            }
            else if(command is MapMessage)
            {
                frame.SetProperty("transformation", this.mapMarshaler.Name);
            }
			
            // Marshal all properties to the Frame.
            IPrimitiveMap map = command.Properties;
            foreach(string key in map.Keys)
            {
                frame.SetProperty(key, map[key]);
            }

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }

        protected virtual void WriteMessageAck(MessageAck command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("ACK", encodeHeaders);
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", "ignore:" + command.CommandId);
            }   

            frame.SetProperty("message-id", command.LastMessageId.ToString());
            frame.SetProperty("subscription", command.ConsumerId.ToString());

            if(command.TransactionId != null)
            {
                frame.SetProperty("transaction", command.TransactionId.ToString());
            }

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }
        
        protected virtual void WriteConnectionInfo(ConnectionInfo command, BinaryWriter dataOut)
        {
            // lets force a receipt for the Connect Frame.
            StompFrame frame = new StompFrame("CONNECT", encodeHeaders);

            frame.SetProperty("client-id", command.ClientId);
            if(!String.IsNullOrEmpty(command.UserName))
            {
                frame.SetProperty("login", command.UserName);
            }
            if(!String.IsNullOrEmpty(command.Password))
            {
                frame.SetProperty("passcode", command.Password);
            }
            frame.SetProperty("host", command.Host);
            frame.SetProperty("accept-version", "1.0,1.1");

            if(MaxInactivityDuration != 0)
            {
                frame.SetProperty("heart-beat", WriteCheckInterval + "," + ReadCheckInterval);
            }

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            this.connectedResponseId = command.CommandId;

            frame.ToStream(dataOut);
        }

        protected virtual void WriteShutdownInfo(ShutdownInfo command, BinaryWriter dataOut)
        {
            System.Diagnostics.Debug.Assert(!command.ResponseRequired);

            StompFrame frame = new StompFrame("DISCONNECT", encodeHeaders);

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }

        protected virtual void WriteConsumerInfo(ConsumerInfo command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("SUBSCRIBE", encodeHeaders);

            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("destination", Destination.ConvertToStompString(command.Destination));
            frame.SetProperty("id", command.ConsumerId.ToString());
            frame.SetProperty("durable-subscriber-name", command.SubscriptionName);
            frame.SetProperty("selector", command.Selector);
            frame.SetProperty("ack", StompHelper.ToStomp(command.AckMode));

            if(command.NoLocal)
            {
                frame.SetProperty("no-local", command.NoLocal.ToString());
            }

            // ActiveMQ extensions to STOMP

            if(command.Transformation != null)
            {
                frame.SetProperty("transformation", command.Transformation);
            }
            else
            {
                frame.SetProperty("transformation", "jms-xml");
            }

            frame.SetProperty("activemq.dispatchAsync", command.DispatchAsync);

            if(command.Exclusive)
            {
                frame.SetProperty("activemq.exclusive", command.Exclusive);
            }

            if(command.SubscriptionName != null)
            {
                frame.SetProperty("activemq.subscriptionName", command.SubscriptionName);
                // For an older 4.0 broker we need to set this header so they get the
                // subscription as well..
                frame.SetProperty("activemq.subcriptionName", command.SubscriptionName);
            }

            frame.SetProperty("activemq.maximumPendingMessageLimit", command.MaximumPendingMessageLimit);
            frame.SetProperty("activemq.prefetchSize", command.PrefetchSize);
            frame.SetProperty("activemq.priority", command.Priority);
            
            if(command.Retroactive)
            {
                frame.SetProperty("activemq.retroactive", command.Retroactive);
            }

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }

        protected virtual void WriteKeepAliveInfo(KeepAliveInfo command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame(StompFrame.KEEPALIVE, encodeHeaders);

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }

        protected virtual void WriteRemoveInfo(RemoveInfo command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("UNSUBSCRIBE", encodeHeaders);
            object id = command.ObjectId;

            if(id is ConsumerId)
            {
                ConsumerId consumerId = id as ConsumerId;
                if(command.ResponseRequired)
                {
                    frame.SetProperty("receipt", command.CommandId);
                }                
                frame.SetProperty("id", consumerId.ToString() );

                if(Tracer.IsDebugEnabled)
                {
                    Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
                }

                frame.ToStream(dataOut);
            }
        }

        protected virtual void WriteTransactionInfo(TransactionInfo command, BinaryWriter dataOut)
        {
            string type = "BEGIN";
            TransactionType transactionType = (TransactionType) command.Type;
            switch(transactionType)
            {
                case TransactionType.Commit:
                    command.ResponseRequired = true;
                    type = "COMMIT";
                    break;
                case TransactionType.Rollback:
                    command.ResponseRequired = true;
                    type = "ABORT";
                    break;
            }

            Tracer.Debug("StompWireFormat - For transaction type: " + transactionType + 
                         " we are using command type: " + type);

            StompFrame frame = new StompFrame(type, encodeHeaders);
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("transaction", command.TransactionId.ToString());

            if(Tracer.IsDebugEnabled)
            {
                Tracer.Debug("StompWireFormat - Writing " + frame.ToString());
            }

            frame.ToStream(dataOut);
        }

        protected virtual void SendCommand(Command command)
        {
            if(transport == null)
            {
                Tracer.Fatal("No transport configured so cannot return command: " + command);
            }
            else
            {
                transport.Command(transport, command);
            }
        }

        protected virtual string ToString(object value)
        {
            if(value != null)
            {
                return value.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
