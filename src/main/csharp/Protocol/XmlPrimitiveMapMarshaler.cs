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
using System.Text;
using System.Xml;
using System.Collections;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp.Protocol
{
    /// <summary>
    /// Reads / Writes an IPrimitveMap as XML.
    /// </summary>
    public class XmlPrimitiveMapMarshaler : IPrimitiveMapMarshaler
    {
        private Encoding encoder = new UTF8Encoding();

        public XmlPrimitiveMapMarshaler() : base()
        {
        }

        public XmlPrimitiveMapMarshaler(Encoding encoder) : base()
        {
            this.encoder = encoder;
        }

        public string Name
        {
            get{ return "jms-map-xml"; }
        }

        public byte[] Marshal(IPrimitiveMap map)
        {
            StringBuilder builder = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings();

            settings.OmitXmlDeclaration = true;
            settings.Encoding = this.encoder;
            settings.NewLineHandling = NewLineHandling.None;

            XmlWriter writer = XmlWriter.Create(builder, settings);

            writer.WriteStartElement("map");

            foreach(String entry in map.Keys)
            {
                writer.WriteStartElement("entry");

                // Encode the Key <string>key</string>
                writer.WriteElementString("string", entry);

                Object value = map[entry];

                // Encode the Value <${type}>value</${type}>
                MarshalPrimitive(writer, value);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Close();

            Console.WriteLine("XML Map = " + builder.ToString());

            return this.encoder.GetBytes(builder.ToString());
        }

        public IPrimitiveMap Unmarshal(byte[] mapContent)
        {
            string content = this.encoder.GetString(mapContent);

            PrimitiveMap result = new PrimitiveMap();

            if(content == null || content == "")
            {
                return result;
            }

            return result;
        }

        private void MarshalPrimitive(XmlWriter writer, Object value)
        {
            if(value == null)
            {
                Console.WriteLine("Null Map Value");
                throw new NullReferenceException("PrimitiveMap values should not be Null");
            }
            else if(value is bool)
            {
                writer.WriteElementString("boolean", value.ToString().ToLower());
            }
            else if(value is byte)
            {
                writer.WriteElementString("byte", value.ToString());
            }
            else if(value is short)
            {
                writer.WriteElementString("short", value.ToString());
            }
            else if(value is int)
            {
                writer.WriteElementString("int", value.ToString());
            }
            else if(value is long)
            {
                writer.WriteElementString("long", value.ToString());
            }
            else if(value is float)
            {
                writer.WriteElementString("float", value.ToString());
            }
            else if(value is double)
            {
                writer.WriteElementString("double", value.ToString());
            }
            else if(value is byte[])
            {
                writer.WriteElementString("byte-array", Convert.ToBase64String((byte[]) value));
            }
            else if(value is string)
            {
                writer.WriteElementString("string", (string) value);
            }
            else if(value is IDictionary)
            {
                Console.WriteLine("Can't Marshal a Dictionary");

                throw new NotSupportedException("Can't marshal nested Maps in Stomp");
            }
            else if(value is IList)
            {
                Console.WriteLine("Can't Marshal a List");

                throw new NotSupportedException("Can't marshal nested Maps in Stomp");
            }
            else
            {
                Console.WriteLine("Can't Marshal a something other than a Primitive Value.");

                throw new Exception("Object is not a primitive: " + value);
            }
        }
    }
}
