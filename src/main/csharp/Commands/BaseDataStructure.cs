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

using System;
using Apache.NMS.Stomp.Protocol;

namespace Apache.NMS.Stomp.Commands
{

    /// <summary>
    /// Base class for all DataStructure implementations
    /// </summary>
    public abstract class BaseDataStructure : DataStructure, ICloneable
    {
        public virtual byte GetDataStructureType()
        {
            return 0;
        }

        public virtual bool IsMarshallAware()
        {
            return false;
        }

        public virtual void BeforeMarshall(StompWireFormat wireFormat)
        {
        }

        public virtual void AfterMarshall(StompWireFormat wireFormat)
        {
        }

        public virtual void BeforeUnmarshall(StompWireFormat wireFormat)
        {
        }

        public virtual void AfterUnmarshall(StompWireFormat wireFormat)
        {
        }

        public virtual void SetMarshalledForm(StompWireFormat wireFormat, byte[] data)
        {
        }

        public virtual byte[] GetMarshalledForm(StompWireFormat wireFormat)
        {
            return null;
        }

        // Helper methods
        public int HashCode(object value)
        {
            if(value != null)
            {
                return value.GetHashCode();
            }

            return -1;
        }

        public virtual Object Clone()
        {
            // Since we are the lowest level base class, do a
            // shallow copy which will include the derived classes.
            // From here we would do deep cloning of other objects
            // if we had any.
            return this.MemberwiseClone();
        }

    }
}
