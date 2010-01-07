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

using NUnit.Framework;
using System;
using System.Text;
using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp.Test.Commands
{
    [TestFixture]
    public class BytesMessageTest
    {
        [Test]
        public void TestCommand()
        {
            BytesMessage message = new BytesMessage();

            // Test that a BytesMessage is created in WriteOnly mode.
            try
            {
                byte[] content = message.Content;
                content.SetValue(0, 0);
                Assert.Fail("Should have thrown an exception");
            }
            catch
            {
            }

            Assert.IsTrue( !message.ReadOnlyBody );
            Assert.IsTrue( !message.ReadOnlyProperties );

            message.Reset();

            Assert.IsNull( message.Content );
            Assert.IsTrue( message.ReadOnlyBody );
        }

        [Test]
        public void TestGetBodyLength()
        {
            BytesMessage msg = new BytesMessage();
            int len = 10;

            for(int i = 0; i < len; i++)
            {
                msg.WriteInt64(5L);
            }

            msg.Reset();
            Assert.IsTrue(msg.BodyLength == (len * 8));
        }

        [Test]
        public void TestReadBoolean()
        {
            BytesMessage msg = new BytesMessage();
            msg.WriteBoolean(true);
            msg.Reset();
            Assert.IsTrue(msg.ReadBoolean());
        }

        [Test]
        public void TestReadByte()
        {
            BytesMessage msg = new BytesMessage();
            msg.WriteByte( (byte)2 );
            msg.Reset();
            Assert.IsTrue( msg.ReadByte() == 2 );
        }

        [Test]
        public void TestReadShort() {
            BytesMessage msg = new BytesMessage();
            msg.WriteInt16((short) 3000);
            msg.Reset();
            Assert.IsTrue(msg.ReadInt16() == 3000);
        }

        [Test]
        public void TestReadChar() {
            BytesMessage msg = new BytesMessage();
            msg.WriteChar('a');
            msg.Reset();
            Assert.IsTrue(msg.ReadChar() == 'a');
        }

        [Test]
        public void TestReadInt() {
            BytesMessage msg = new BytesMessage();
            msg.WriteInt32(3000);
            msg.Reset();
            Assert.IsTrue(msg.ReadInt32() == 3000);
        }

        [Test]
        public void TestReadLong() {
            BytesMessage msg = new BytesMessage();
            msg.WriteInt64(3000);
            msg.Reset();
            Assert.IsTrue(msg.ReadInt64() == 3000);
        }

        [Test]
        public void TestReadFloat() {
            BytesMessage msg = new BytesMessage();
            msg.WriteSingle(3.3f);
            msg.Reset();
            Assert.IsTrue(msg.ReadSingle() == 3.3f);
        }

        [Test]
        public void TestReadDouble() {
            BytesMessage msg = new BytesMessage();
            msg.WriteDouble(3.3d);
            msg.Reset();
            Assert.IsTrue(msg.ReadDouble() == 3.3d);
        }

        [Test]
        public void TestReadString() {
            BytesMessage msg = new BytesMessage();
            string str = "this is a test";
            msg.WriteString(str);
            msg.Reset();
            Assert.IsTrue(msg.ReadString() == str);
        }

        [Test]
        public void TestReadBytesbyteArray()
        {
            BytesMessage msg = new BytesMessage();
            byte[] data = new byte[50];
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = (byte) i;
            }
            msg.WriteBytes(data);
            msg.Reset();
            byte[] test = new byte[data.Length];
            msg.ReadBytes(test);
            for(int i = 0; i < test.Length; i++)
            {
                Assert.IsTrue(test[i] == i);
            }
        }

        [Test]
        public void TestWriteObject()
        {
            BytesMessage msg = new BytesMessage();

            try
            {
                msg.WriteObject("fred");
                msg.WriteObject((Boolean) true);
                msg.WriteObject((Char) 'q');
                msg.WriteObject((Byte) ((byte) 1));
                msg.WriteObject((Int16) ((short) 3));
                msg.WriteObject((Int32) 3 );
                msg.WriteObject((Int64) 300L);
                msg.WriteObject((Single) 3.3f );
                msg.WriteObject((Double) 3.3 );
                msg.WriteObject((Object) new byte[3]);
            }
            catch(MessageFormatException)
            {
                Assert.Fail("objectified primitives should be allowed");
            }

            try
            {
                msg.WriteObject(new Object());
                Assert.Fail("only objectified primitives are allowed");
            }
            catch(MessageFormatException )
            {
            }
        }

        [Test]
        public void TestClearBody() {
            BytesMessage bytesMessage = new BytesMessage();
            try {
                bytesMessage.WriteInt32(1);
                bytesMessage.ClearBody();
                Assert.IsFalse(bytesMessage.ReadOnlyBody);
                bytesMessage.WriteInt32(1);
                bytesMessage.ReadInt32();
            }
            catch(MessageNotReadableException)
            {
            }
            catch(MessageNotWriteableException)
            {
                Assert.IsTrue(false);
            }
        }

        [Test]
        public void TestReset() {

            BytesMessage message = new BytesMessage();

            try
            {
                message.WriteDouble(24.5);
                message.WriteInt64(311);
            }
            catch(MessageNotWriteableException)
            {
                Assert.Fail("should be writeable");
            }

            message.Reset();

            try {
                Assert.IsTrue(message.ReadOnlyBody);
                Assert.AreEqual(message.ReadDouble(), 24.5, 0);
                Assert.AreEqual(message.ReadInt64(), 311);
            }
            catch(MessageNotReadableException)
            {
                Assert.Fail("should be readable");
            }

            try
            {
                message.WriteInt32(33);
                Assert.Fail("should throw exception");
            }
            catch(MessageNotWriteableException)
            {
            }
        }

        [Test]
        public void TestReadOnlyBody()
        {
            BytesMessage message = new BytesMessage();
            try {
                message.WriteBoolean(true);
                message.WriteByte((byte) 1);
                message.WriteBytes(new byte[1]);
                message.WriteBytes(new byte[3], 0, 2);
                message.WriteChar('a');
                message.WriteDouble(1.5);
                message.WriteSingle((float) 1.5);
                message.WriteInt32(1);
                message.WriteInt64(1);
                message.WriteObject("stringobj");
                message.WriteInt16((short) 1);
                message.WriteString("utfstring");
            }
            catch(MessageNotWriteableException)
            {
                Assert.Fail("Should be writeable");
            }

            message.Reset();

            try
            {
                message.ReadBoolean();
                message.ReadByte();
                message.ReadBytes(new byte[1]);
                message.ReadBytes(new byte[2], 2);
                message.ReadChar();
                message.ReadDouble();
                message.ReadSingle();
                message.ReadInt32();
                message.ReadInt64();
                message.ReadString();
                message.ReadInt16();
                message.ReadString();
            }
            catch(MessageNotReadableException)
            {
                Assert.Fail("Should be readable");
            }

            try
            {
                message.WriteBoolean(true);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteByte((byte) 1);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteBytes(new byte[1]);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteBytes(new byte[3], 0, 2);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteChar('a');
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteDouble(1.5);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteSingle((float) 1.5);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteInt32(1);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteInt64(1);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteObject("stringobj");
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteInt16((short) 1);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }

            try
            {
                message.WriteString("utfstring");
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotWriteableException)
            {
            }
        }

        [Test]
        public void TestWriteOnlyBody()
        {
            BytesMessage message = new BytesMessage();
            message.ClearBody();

            try
            {
                message.WriteBoolean(true);
                message.WriteByte((byte) 1);
                message.WriteBytes(new byte[1]);
                message.WriteBytes(new byte[3], 0, 2);
                message.WriteChar('a');
                message.WriteDouble(1.5);
                message.WriteSingle((float) 1.5);
                message.WriteInt32(1);
                message.WriteInt64(1);
                message.WriteObject("stringobj");
                message.WriteInt16((short) 1);
                message.WriteString("utfstring");
            }
            catch(MessageNotWriteableException)
            {
                Assert.Fail("Should be writeable");
            }

            try
            {
                message.ReadBoolean();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadByte();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadBytes(new byte[1]);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadBytes(new byte[2], 2);
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadChar();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadDouble();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadSingle();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadInt32();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadInt64();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadString();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadInt16();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }

            try
            {
                message.ReadString();
                Assert.Fail("Should have thrown exception");
            }
            catch(MessageNotReadableException)
            {
            }
        }

    }
}
