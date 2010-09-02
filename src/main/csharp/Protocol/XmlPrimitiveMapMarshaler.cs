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
using System.IO;
using System.Xml;
using System.Collections;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp.Protocol
{
    /// <summary>
    /// Reads / Writes an IPrimitveMap as XML compatible with XStream.
    /// </summary>
    public class XmlPrimitiveMapMarshaler : IPrimitiveMapMarshaler
    {
        private readonly Encoding encoder = new UTF8Encoding();

        public XmlPrimitiveMapMarshaler()
        {
        }

        public XmlPrimitiveMapMarshaler(Encoding encoder)
        {
            this.encoder = encoder;
        }

        public string Name
        {
            get{ return "jms-map-xml"; }
        }

        public byte[] Marshal(IPrimitiveMap map)
        {
            if(map == null)
            {
                return null;
            }

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

            return this.encoder.GetBytes(builder.ToString());
        }

        public IPrimitiveMap Unmarshal(byte[] mapContent)
        {
            string xmlString = this.encoder.GetString(mapContent, 0, mapContent.Length);

            PrimitiveMap result = new PrimitiveMap();

            if (xmlString == null || xmlString == "")
            {
                return result;
            }

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            settings.IgnoreProcessingInstructions = true;

            XmlReader reader = XmlReader.Create(new StringReader(xmlString), settings);

            reader.MoveToContent();
            reader.ReadStartElement("map");

            while(reader.Name == "entry")
            {
                reader.ReadStartElement();
                string key = reader.ReadElementContentAsString("string", "");

                Object value = null;

                switch(reader.Name)
                {
                case "char":
                    value = Convert.ToChar(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "double":
                    value = Convert.ToDouble(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "float":
                    value = Convert.ToSingle(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "long":
                    value = Convert.ToInt64(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "int":
                    value = Convert.ToInt32(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "short":
                    value = Convert.ToInt16(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "byte":
                    value = (byte) Convert.ToInt16(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "boolean":
                    value = Convert.ToBoolean(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                case "byte-array":
                    value = Convert.FromBase64String(reader.ReadElementContentAsString());
                    reader.ReadEndElement();
                    break;
                default:
                    value = reader.ReadElementContentAsString();
                    reader.ReadEndElement();
                    break;
                };

                // Now store the value into our new PrimitiveMap.
                result[key] = value;
            }

            reader.Close();

            return result;
        }

        private void MarshalPrimitive(XmlWriter writer, Object value)
        {
            if(value == null)
            {
                throw new NullReferenceException("PrimitiveMap values should not be Null");
            }
            else if(value is char)
            {
                writer.WriteElementString("char", value.ToString());
            }
            else if(value is bool)
            {
                writer.WriteElementString("boolean", value.ToString().ToLower());
            }
            else if(value is byte)
            {
                writer.WriteElementString("byte", ((Byte) value).ToString());
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
                Tracer.Debug("Can't Marshal a Dictionary");

                throw new NotSupportedException("Can't marshal nested Maps in Stomp");
            }
            else if(value is IList)
            {
                Tracer.Debug("Can't Marshal a List");

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
