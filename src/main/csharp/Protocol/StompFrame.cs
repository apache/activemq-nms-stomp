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
using System.Collections;
using System.IO;
using System.Text;

namespace Apache.NMS.Stomp.Protocol
{
    public class StompFrame
    {
        /// Used to terminate a header line or end of a headers section of the Frame.
        public const String NEWLINE = "\n";
        /// Used to seperate the Key / Value pairing in Frame Headers
        public const String SEPARATOR = ":";
        /// Used to mark the End of the Frame.
        public const byte FRAME_TERMINUS = (byte) 0;
        /// Used to denote a Special KeepAlive command that consists of a single newline.
        public const String KEEPALIVE = "KEEPALIVE";

        public const byte BREAK = (byte)('\n');
        public const byte COLON = (byte)(':');
        public const byte ESCAPE = (byte)('\\');
        public readonly byte[] ESCAPE_ESCAPE_SEQ = new byte[2]{ 92, 92 };
        public readonly byte[] COLON_ESCAPE_SEQ = new byte[2]{ 92, 99 };
        public readonly byte[] NEWLINE_ESCAPE_SEQ = new byte[2]{ 92, 110 };

        private string command;
        private IDictionary properties = new Hashtable();
        private byte[] content;
        private bool encodingEnabled;

        private readonly Encoding encoding = new UTF8Encoding();
        
        public StompFrame()
        {
        }

        public StompFrame(bool encodingEnabled)
        {
            this.encodingEnabled = encodingEnabled;
        }

        public StompFrame(string command)
        {
            this.command = command;
        }

        public StompFrame(string command, bool encodingEnabled)
        {
            this.command = command;
            this.encodingEnabled = encodingEnabled;
        }

        public bool EncodingEnabled
        {
            get { return this.encodingEnabled; }
            set { this.encodingEnabled = value; }
        }
        
        public byte[] Content
        {
            get { return this.content; }
            set { this.content = value; }
        }

        public string Command
        {
            get { return this.command; }
            set { this.command = value; }
        }

        public IDictionary Properties
        {
            get { return this.properties; }
            set { this.properties = value; }
        }

        public bool HasProperty(string name)
        {
            return this.properties.Contains(name);
        }

        public void SetProperty(string name, Object value)
        {
            if(value == null)
            {
                return;
            }
            
            this.Properties[name] = value.ToString();
        }

        public string GetProperty(string name)
        {
            return GetProperty(name, null);
        }

        public string GetProperty(string name, string fallback)
        {
            if(this.properties.Contains(name))
            {
                return this.properties[name] as string;
            }

            return fallback;
        }

        public string RemoveProperty(string name)
        {
            string result = null;
            
            if(this.properties.Contains(name))
            {
                result = this.properties[name] as string;
                this.properties.Remove(name);
            }

            return result;
        }

        public void ClearProperties()
        {
            this.properties.Clear();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(GetType().Name + "[ ");
            builder.Append("Command=" + Command);
            builder.Append(", Properties={");
            foreach(string key in this.properties.Keys)
            {
                builder.Append(" " + key + "=" + this.properties[key]);
            }

            builder.Append("}, ");
            builder.Append("Content=" + this.content ?? this.content.ToString());
            builder.Append("]");

            return builder.ToString();
        }

        public void ToStream(BinaryWriter dataOut)
        {
            if(this.Command == KEEPALIVE)
            {
                dataOut.Write(BREAK);
                dataOut.Flush();
                return;
            }

            StringBuilder builder = new StringBuilder();
            
            builder.Append(this.Command);
            builder.Append(NEWLINE);

            foreach(String key in this.Properties.Keys)
            {
                builder.Append(key);
                builder.Append(SEPARATOR);
                builder.Append(EncodeHeader(this.Properties[key] as string));
                builder.Append(NEWLINE);
            }

            builder.Append(NEWLINE);
            dataOut.Write(this.encoding.GetBytes(builder.ToString()));
            
            if(this.Content != null)
            {
                dataOut.Write(this.Content);
            }

            dataOut.Write(FRAME_TERMINUS);
        }

        public void FromStream(BinaryReader dataIn)
        {
            this.ReadCommandHeader(dataIn);

            if(this.command != KEEPALIVE)
            {
                this.ReadHeaders(dataIn);
                this.ReadContent(dataIn);
            }
        }

        private void ReadCommandHeader(BinaryReader dataIn)
        {
            this.command = ReadLine(dataIn);

            if(String.IsNullOrEmpty(this.command))
            {
                this.command = "KEEPALIVE";
            }
        }

        private void ReadHeaders(BinaryReader dataIn)
        {
            string line;
            while((line = ReadLine(dataIn)) != "")
            {
                int idx = line.IndexOf(':');
                
                if(idx > 0)
                {
                    string key = line.Substring(0, idx);
                    string value = line.Substring(idx + 1);

                    // Stomp v1.1+ allows multiple copies of a property, the first
                    // one is considered to be the newest, we could figure out how
                    // to store them all but for now we just throw the rest out.
                    if(!this.properties.Contains(key))
                    {
                        this.properties[key] = DecodeHeader(value);
                    }
                }
                else
                {
                    Tracer.Debug("StompFrame - Read Malformed Header: " + line);
                }
            }            
        }

        private void ReadContent(BinaryReader dataIn)
        {
            if(this.properties.Contains("content-length"))
            {
                int size = Int32.Parse(this.properties["content-length"] as string);
                this.content = dataIn.ReadBytes(size);
                
                // Read the terminating NULL byte for this frame.                
                if(dataIn.Read() != 0)
                {
                    Tracer.Debug("StompFrame - Error Invalid Frame, no trailing Null.");
                }
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                int nextChar;
                while((nextChar = dataIn.ReadByte()) != 0)
                {
                    // The first Null in this case marks the end of data.
                    if(nextChar < 0)
                    {
                        break;
                    }
                    
                    ms.WriteByte((byte)nextChar);
                }
                
                this.content = ms.ToArray();
            }            
        }

        private String ReadLine(BinaryReader dataIn)
        {
            MemoryStream ms = new MemoryStream();
            
            while(true)
            {
                int nextChar = dataIn.Read();
                if(nextChar < 0)
                {
                    throw new IOException("Peer closed the stream.");
                }
                if(nextChar == 10)
                {
                    break;
                }
                ms.WriteByte((byte)nextChar);
            }
            
            byte[] data = ms.ToArray();
            return encoding.GetString(data, 0, data.Length);
        }

        private String EncodeHeader(String header)
        {
            String result = header;
            if(this.encodingEnabled)
            {
                byte[] utf8buf = this.encoding.GetBytes(header);
                MemoryStream stream = new MemoryStream(utf8buf.Length);
                foreach(byte val in utf8buf)
                {
                    switch(val)
                    {
                    case ESCAPE:
                        stream.Write(ESCAPE_ESCAPE_SEQ, 0, ESCAPE_ESCAPE_SEQ.Length);
                        break;
                    case BREAK:
                        stream.Write(NEWLINE_ESCAPE_SEQ, 0, NEWLINE_ESCAPE_SEQ.Length);
                        break;
                    case COLON:
                        stream.Write(COLON_ESCAPE_SEQ, 0, COLON_ESCAPE_SEQ.Length);
                        break;
                    default:
                        stream.WriteByte(val);
                        break;
                    }
                }

                byte[] data = stream.ToArray();
                result = encoding.GetString(data, 0, data.Length);
            }

            return result;
        }

        private String DecodeHeader(String header)
        {
            MemoryStream decoded = new MemoryStream();

            int value = -1;
            byte[] utf8buf = this.encoding.GetBytes(header);
            MemoryStream stream = new MemoryStream(utf8buf);

            while((value = stream.ReadByte()) != -1)
            {
                if(value == 92)
                {
                    int next = stream.ReadByte();
                    if (next != -1)
                    {
                        switch(next) {
                        case 110:
                            decoded.WriteByte(BREAK);
                            break;
                        case 99:
                            decoded.WriteByte(COLON);
                            break;
                        case 92:
                            decoded.WriteByte(ESCAPE);
                            break;
                        default:
                            stream.Seek(-1, SeekOrigin.Current);
                            decoded.WriteByte((byte)value);
                            break;
                        }
                    }
                    else
                    {
                        decoded.WriteByte((byte)value);
                    }

                }
                else
                {
                    decoded.WriteByte((byte)value);
                }
            }

            byte[] data = decoded.ToArray();
            return encoding.GetString(data, 0, data.Length);
        }
    }
}
