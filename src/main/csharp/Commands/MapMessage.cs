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

using Apache.NMS.Util;
using Apache.NMS.Stomp.Protocol;

namespace Apache.NMS.Stomp.Commands
{
    public class MapMessage : Message, IMapMessage
    {
        private PrimitiveMap body;
        private PrimitiveMapInterceptor typeConverter;

        public MapMessage()
        {
        }

        public MapMessage(PrimitiveMap body)
        {
            this.body = body;
            this.typeConverter = new PrimitiveMapInterceptor(this, this.body);
        }

        public override byte GetDataStructureType()
        {
            return DataStructureTypes.MapMessageType;
        }

        public override void ClearBody()
        {
            this.body = null;
            this.typeConverter = null;
            base.ClearBody();
        }

        public override bool ReadOnlyBody
        {
            get { return base.ReadOnlyBody; }

            set
            {
                if(this.typeConverter != null)
                {
                    this.typeConverter.ReadOnly = true;
                }

                base.ReadOnlyBody = value;
            }
        }


        public IPrimitiveMap Body
        {
            get
            {
                if(this.body == null)
                {
                    this.body = new PrimitiveMap();
                    this.typeConverter = new PrimitiveMapInterceptor(this, this.body);
                }

                return this.typeConverter;
            }

            set
            {
                this.body = value as PrimitiveMap;
                this.typeConverter = value != null ? new PrimitiveMapInterceptor(this, value) : null;
            }
        }

        public override void BeforeMarshall(StompWireFormat wireFormat)
        {
            base.BeforeMarshall(wireFormat);

            this.Content = body == null ? null : wireFormat.MapMarshaler.Marshal(body);
        }
    }
}
