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

using Apache.NMS.Stomp.State;

namespace Apache.NMS.Stomp.Commands
{
    public class WireFormatInfo : BaseCommand
    {
        private long writeCheckInterval = 0;
        private long readCheckInterval = 0;
        private float version = 1.0f;
        private string session;

        public WireFormatInfo()
        {
        }

        public long WriteCheckInterval
        {
            get { return this.writeCheckInterval; }
            set { this.writeCheckInterval = value; }
        }

        public long ReadCheckInterval
        {
            get { return this.readCheckInterval; }
            set { this.readCheckInterval = value; }
        }

        public float Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        public string Session
        {
            get { return this.session; }
            set { this.session = value; }
        }

        /// <summery>
        /// Get the unique identifier that this object and its own
        /// Marshaler share.
        /// </summery>
        public override byte GetDataStructureType()
        {
            return DataStructureTypes.TransactionInfoType;
        }

        /// <summery>
        /// Returns a string containing the information for this DataStructure
        /// such as its type and value of its elements.
        /// </summery>
        public override string ToString()
        {
            return GetType().Name + "[" +
                "WriteCheckInterval=" + WriteCheckInterval + ", " +
                "ReadCheckInterval=" + ReadCheckInterval + ", " +
                "Session=" + Session + ", " +
                "Version=" + Version +
                "]";
        }

        /// <summery>
        /// Return an answer of true to the IsWireFormatInfo() query.
        /// </summery>
        public override bool IsWireFormatInfo
        {
            get
            {
                return true;
            }
        }

        public override Response visit(ICommandVisitor visitor)
        {
            return null;
        }
    }
}

