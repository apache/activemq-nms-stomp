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
using Apache.NMS;
using System;
using System.Collections;
using System.IO;
using System.Text;

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
            StompFrame frame = new StompFrame();
            frame.FromStream(dataIn);
            
            Object answer = CreateCommand(frame);
            return answer;
        }

        protected virtual Object CreateCommand(StompFrame frame)
        {
            string command = frame.Command;
            
            if(command == "RECEIPT" || command == "CONNECTED")
            {
                string text = frame.RemoveProperty("receipt-id");
                if(text != null)
                {
                    Response answer = new Response();
                    if(text.StartsWith("ignore:"))
                    {
                        text = text.Substring("ignore:".Length);
                    }

                    Tracer.Debug("StompWireFormat - Received RESPONSE command: CorrelationId = " + text);
                    
                    answer.CorrelationId = Int32.Parse(text);
                    return answer;
                }
                else if(command == "CONNECTED")
                {
                    text = frame.RemoveProperty("response-id");

                    Tracer.Debug("StompWireFormat - Received CONNECTED command: ResponseId = " + text);
                    
                    if(text != null)
                    {
                        Response answer = new Response();
                        answer.CorrelationId = Int32.Parse(text);
                        return answer;
                    }
                }
            }
            else if(command == "ERROR")
            {
                string text = frame.RemoveProperty("receipt-id");
                
                if(text != null && text.StartsWith("ignore:"))
                {
                    Tracer.Debug("StompWireFormat - Received ERROR Response command: correlationId = " + text);
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
                    Tracer.Debug("StompWireFormat - Received ERROR command: " + error.Message);                    
                    return answer;
                }
            }
            else if(command == "MESSAGE")
            {
                Tracer.Debug("StompWireFormat - Received MESSAGE command");
                return ReadMessage(frame);
            }
            
            Tracer.Error("Unknown command: " + frame.Command + " headers: " + frame.Properties);
            
            return null;
        }

        protected virtual Command ReadMessage(StompFrame frame)
        {
            Message message = null;
            if(frame.HasProperty("content-length"))
            {
                message = new BytesMessage();
                message.Content = frame.Content;
            }
            else
            {
                message = new TextMessage(encoder.GetString(frame.Content, 0, frame.Content.Length));
            }

            // Remove any receipt header we might have attached if the outbound command was
            // sent with response required set to true
            frame.RemoveProperty("receipt");

            message.Type = frame.RemoveProperty("type");
            message.Destination = StompHelper.ToDestination(frame.RemoveProperty("destination"));
            message.ReplyTo = StompHelper.ToDestination(frame.RemoveProperty("reply-to"));
            message.TargetConsumerId = StompHelper.ToConsumerId(frame.RemoveProperty("subscription"));
            message.CorrelationId = frame.RemoveProperty("correlation-id");

            Tracer.Debug("RECV - Inbound MessageId = " + frame.GetProperty("message-id"));
          
            message.MessageId = StompHelper.ToMessageId(frame.RemoveProperty("message-id"));
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
            StompFrame frame = new StompFrame("SEND");
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("destination", StompHelper.ToStomp(command.Destination));
            
            if(command.ReplyTo != null)
            {
                frame.SetProperty("reply-to", StompHelper.ToStomp(command.ReplyTo));
            }
            if(command.CorrelationId != null )
            {
                frame.SetProperty("correlation-id", command.CorrelationId);
            }
            if(command.Expiration != 0)
            {
                frame.SetProperty("expires", command.Expiration);
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
                frame.SetProperty("transaction", StompHelper.ToStomp(command.TransactionId));
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

            frame.ToStream(dataOut);
        }

        protected virtual void WriteMessageAck(MessageAck command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("ACK");
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", "ignore:" + command.CommandId);
            }   

            frame.SetProperty("message-id", StompHelper.ToStomp(command.LastMessageId));

            Tracer.Debug("ACK - Outbound MessageId = " + frame.GetProperty("message-id"));
            
            if(command.TransactionId != null)
            {
                frame.SetProperty("transaction", StompHelper.ToStomp(command.TransactionId));
            }

            frame.ToStream(dataOut);
        }
        
        protected virtual void WriteConnectionInfo(ConnectionInfo command, BinaryWriter dataOut)
        {
            // lets force a receipt for the Connect Frame.
            
            StompFrame frame = new StompFrame("CONNECT");

            frame.SetProperty("client-id", command.ClientId);
            frame.SetProperty("login", command.UserName);
            frame.SetProperty("passcode", command.Password);
            frame.SetProperty("request-id", command.CommandId);

            frame.ToStream(dataOut);
        }

        protected virtual void WriteShutdownInfo(ShutdownInfo command, BinaryWriter dataOut)
        {
            System.Diagnostics.Debug.Assert(!command.ResponseRequired);
            
            new StompFrame("DISCONNECT").ToStream(dataOut);
        }

        protected virtual void WriteConsumerInfo(ConsumerInfo command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("SUBSCRIBE");

            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("destination", StompHelper.ToStomp(command.Destination));
            frame.SetProperty("id", StompHelper.ToStomp(command.ConsumerId));
            frame.SetProperty("durable-subscriber-name", command.SubscriptionName);
            frame.SetProperty("selector", command.Selector);
            frame.SetProperty("ack", StompHelper.ToStomp(command.AckMode));

            Tracer.Debug("SUBSCRIBE : Outbound AckMode = " + frame.GetProperty("ack"));
            
            if(command.NoLocal)
            {
                frame.SetProperty("no-local", command.NoLocal.ToString());
            }

            // ActiveMQ extensions to STOMP
            // frame.SetProperty("transformation", "jms-map-xml");
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

            frame.ToStream(dataOut);
        }

        protected virtual void WriteRemoveInfo(RemoveInfo command, BinaryWriter dataOut)
        {
            StompFrame frame = new StompFrame("UNSUBSCRIBE");
            object id = command.ObjectId;

            if(id is ConsumerId)
            {
                ConsumerId consumerId = id as ConsumerId;
                if(command.ResponseRequired)
                {
                    frame.SetProperty("receipt", command.CommandId);
                }                
                frame.SetProperty("id", StompHelper.ToStomp(consumerId));
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

            StompFrame frame = new StompFrame(type);
            if(command.ResponseRequired)
            {
                frame.SetProperty("receipt", command.CommandId);
            }
            
            frame.SetProperty("transaction", StompHelper.ToStomp(command.TransactionId));
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
