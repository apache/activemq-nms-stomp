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
using Apache.NMS;
using Apache.NMS.Util;
using Apache.NMS.Test;
using Apache.NMS.Stomp.Protocol;
using NUnit.Framework;

namespace Apache.NMS.Stomp.Test.Protocol
{
    [TestFixture]
    public class XmlPrimitiveMapMarshalerTest
    {
        private const String xmlString =
                @"<map>
                      <entry>
                        <string>BYTES</string>
                        <byte-array>CgoKFBQU</byte-array>
                      </entry>
                      <entry>
                        <string>CHAR</string>
                        <char>a</char>
                      </entry>
                      <entry>
                        <string>DOUBLE</string>
                        <double>1.5</double>
                      </entry>
                      <entry>
                        <string>BYTE</string>
                        <byte>123</byte>
                      </entry>
                      <entry>
                        <string>SHORT</string>
                        <short>-32768</short>
                      </entry>
                      <entry>
                        <string>INT</string>
                        <int>5</int>
                      </entry>
                      <entry>
                        <string>FLOAT</string>
                        <float>1.1</float>
                      </entry>
                      <entry>
                        <string>LONG</string>
                        <long>256</long>
                      </entry>
                      <entry>
                        <string>STRING</string>
                        <string>FOO</string>
                      </entry>
                      <entry>
                        <string>BOOL</string>
                        <boolean>true</boolean>
                      </entry>
                    </map>";

        [Test]
        public void TestMarshalPrimitiveMap()
        {
            XmlPrimitiveMapMarshaler marshaler = new XmlPrimitiveMapMarshaler();

            PrimitiveMap map = new PrimitiveMap();

            map.SetBool("boolean", true);
            map.SetByte("byte", (byte)1);
            map["bytes1"] = new byte[1];
            map.SetChar("char", 'a');
            map.SetDouble("double", 1.5);
            map.SetFloat("float", 1.5f);
            map.SetInt("int", 1);
            map.SetLong("long", 1);
            map["object"] = "stringObj";
            map.SetShort("short", (short)1);
            map.SetString("string", "string");

            byte[] result = marshaler.Marshal(map);
            Assert.IsNotNull(result);

            result = marshaler.Marshal(null);
            Assert.IsNull(result);

        }

        [Test]
        public void TestUnmarshalPrimitiveMap()
        {
            XmlPrimitiveMapMarshaler marshaler = new XmlPrimitiveMapMarshaler();

            byte[] rawBytes = Encoding.UTF8.GetBytes(xmlString);

            IPrimitiveMap result = marshaler.Unmarshal(rawBytes);

            Assert.IsNotNull(result);

            Assert.IsTrue(result.Contains("BOOL"));
            Assert.IsTrue(result.Contains("BYTES"));
            Assert.IsTrue(result.Contains("STRING"));
            Assert.IsTrue(result.Contains("LONG"));
            Assert.IsTrue(result.Contains("FLOAT"));
            Assert.IsTrue(result.Contains("INT"));
            Assert.IsTrue(result.Contains("BYTE"));
            Assert.IsTrue(result.Contains("SHORT"));
            Assert.IsTrue(result.Contains("DOUBLE"));
            Assert.IsTrue(result.Contains("CHAR"));

        }
    }
}
