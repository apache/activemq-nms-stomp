/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;

using Apache.NMS.ActiveMQ.State;

namespace Apache.NMS.Stomp.Commands
{
    public class RemoveInfo : BaseCommand
    {
        public const byte ID_REMOVEINFO = 12;

        DataStructure objectId;
        long lastDeliveredSequenceId;

        ///
        /// <summery>
        ///  Get the unique identifier that this object and its own
        ///  Marshaler share.
        /// </summery>
        ///
        public override byte GetDataStructureType()
        {
            return ID_REMOVEINFO;
        }

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            return GetType().Name + "[" +
                "ObjectId=" + ObjectId +
                "LastDeliveredSequenceId=" + LastDeliveredSequenceId +
                "]";
        }

        public DataStructure ObjectId
        {
            get { return objectId; }
            set { this.objectId = value; }
        }

        public long LastDeliveredSequenceId
        {
            get { return lastDeliveredSequenceId; }
            set { this.lastDeliveredSequenceId = value; }
        }

        ///
        /// <summery>
        ///  Return an answer of true to the isRemoveInfo() query.
        /// </summery>
        ///
        public override bool IsRemoveInfo
        {
            get
            {
                return true;
            }
        }

    };
}

