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
using System.Collections.Specialized;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp.Commands
{
    /// <summary>
    /// Summary description for Destination.
    /// </summary>
    public abstract class Destination : BaseDataStructure, IDestination
    {
        /// <summary>
        /// Topic Destination object
        /// </summary>
        public const int STOMP_TOPIC = 1;
        /// <summary>
        /// Temporary Topic Destination object
        /// </summary>
        public const int STOMP_TEMPORARY_TOPIC = 2;
        /// <summary>
        /// Queue Destination object
        /// </summary>
        public const int STOMP_QUEUE = 3;
        /// <summary>
        /// Temporary Queue Destination object
        /// </summary>
        public const int STOMP_TEMPORARY_QUEUE = 4;

//        private const String TEMP_PREFIX = "{TD{";
//        private const String TEMP_POSTFIX = "}TD}";
        private const String COMPOSITE_SEPARATOR = ",";

        private String physicalName = "";
        private StringDictionary options = null;
        private bool remoteDestination;

        /// <summary>
        /// The Default Constructor
        /// </summary>
        protected Destination()
        {
        }

        /// <summary>
        /// Construct the Destination with a defined physical name;
        /// </summary>
        /// <param name="name"></param>
        protected Destination(String name)
        {
            SetPhysicalName(name);
        }

        public void Dispose()
        {
        }

        public bool IsTopic
        {
            get
            {
                int destinationType = GetDestinationType();
                return STOMP_TOPIC == destinationType
                    || STOMP_TEMPORARY_TOPIC == destinationType;
            }
        }

        public bool IsQueue
        {
            get
            {
                int destinationType = GetDestinationType();
                return STOMP_QUEUE == destinationType
                    || STOMP_TEMPORARY_QUEUE == destinationType;
            }
        }

        public bool IsTemporary
        {
            get
            {
                int destinationType = GetDestinationType();
                return STOMP_TEMPORARY_QUEUE == destinationType
                    || STOMP_TEMPORARY_TOPIC == destinationType;
            }
        }

        /// <summary>
        /// Dictionary of name/value pairs representing option values specified
        /// in the URI used to create this Destination.  A null value is returned
        /// if no options were specified.
        /// </summary>
        internal StringDictionary Options
        {
            get { return this.options; }
        }

        /// <summary>
        /// Indicates if the Desination was created by this client or was provided
        /// by the broker, most commonly the deinstinations provided by the broker
        /// are those that appear in the ReplyTo field of a Message.
        /// </summary>
        internal bool RemoteDestination
        {
            get { return this.remoteDestination; }
            set { this.remoteDestination = value; }
        }

        private void SetPhysicalName(string name)
        {
            this.physicalName = name;

            int p = name.IndexOf('?');
            if(p >= 0)
            {
                String optstring = physicalName.Substring(p + 1);
                this.physicalName = name.Substring(0, p);
                options = URISupport.ParseQuery(optstring);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static Destination Transform(IDestination destination)
        {
            Destination result = null;
            if(destination != null)
            {
                if(destination is Destination)
                {
                    result = (Destination) destination;
                }
                else
                {
                    if(destination is ITemporaryQueue)
                    {
                        result = new TempQueue(((IQueue) destination).QueueName);
                    }
                    else if(destination is ITemporaryTopic)
                    {
                        result = new TempTopic(((ITopic) destination).TopicName);
                    }
                    else if(destination is IQueue)
                    {
                        result = new Queue(((IQueue) destination).QueueName);
                    }
                    else if(destination is ITopic)
                    {
                        result = new Topic(((ITopic) destination).TopicName);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Create a Destination using the name given, the type is determined by the
        /// value of the type parameter.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pyhsicalName"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static Destination CreateDestination(int type, String pyhsicalName, bool remote)
        {
            Destination result = null;
            if(pyhsicalName == null)
            {
                return null;
            }
            else switch (type)
            {
                case STOMP_TOPIC:
                    result = new Topic(pyhsicalName);
                    break;
                case STOMP_TEMPORARY_TOPIC:
                    result = new TempTopic(pyhsicalName);
                    break;
                case STOMP_QUEUE:
                    result = new Queue(pyhsicalName);
                    break;
                default:
                    result = new TempQueue(pyhsicalName);
                    break;
            }

            result.RemoteDestination = remote;

            return result;
        }


        public static Destination ConvertToDestination(String text)
        {
            if(text == null)
            {
                return null;
            }

            int type = Destination.STOMP_QUEUE;
            string lowertext = text.ToLower();
            bool remote = false;

            if(lowertext.StartsWith("/queue/"))
            {
                text = text.Substring("/queue/".Length);
            }
            else if(lowertext.StartsWith("/topic/"))
            {
                text = text.Substring("/topic/".Length);
                type = Destination.STOMP_TOPIC;
            }
            else if(lowertext.StartsWith("/temp-topic/"))
            {
                text = text.Substring("/temp-topic/".Length);
                type = Destination.STOMP_TEMPORARY_TOPIC;
            }
            else if(lowertext.StartsWith("/temp-queue/"))
            {
                text = text.Substring("/temp-queue/".Length);
                type = Destination.STOMP_TEMPORARY_QUEUE;
            }
            else if(lowertext.StartsWith("/remote-temp-topic/"))
            {
                text = text.Substring("/remote-temp-topic/".Length);
                type = Destination.STOMP_TEMPORARY_TOPIC;
                remote = true;
            }
            else if(lowertext.StartsWith("/remote-temp-queue/"))
            {
                text = text.Substring("/remote-temp-queue/".Length);
                type = Destination.STOMP_TEMPORARY_QUEUE;
                remote = true;
            }

            return Destination.CreateDestination(type, text, remote);
        }

        public static string ConvertToStompString(Destination destination)
        {
            if(destination == null)
            {
                return null;
            }

            string result;

            switch(destination.DestinationType)
            {
                case DestinationType.Topic:
                    result = "/topic/" + destination.PhysicalName;
                    break;
                case DestinationType.TemporaryTopic:
                    result = (destination.RemoteDestination ? "/remote-temp-topic/" : "/temp-topic/") + destination.PhysicalName;
                    break;
                case DestinationType.TemporaryQueue:
                    result = (destination.RemoteDestination ? "/remote-temp-queue/" : "/temp-queue/") + destination.PhysicalName;
                    break;
                default:
                    result = "/queue/" + destination.PhysicalName;
                    break;
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="o">object to compare</param>
        /// <returns>1 if this is less than o else 0 if they are equal or -1 if this is less than o</returns>
        public int CompareTo(Object o)
        {
            if(o is Destination)
            {
                return CompareTo((Destination) o);
            }
            return -1;
        }

        /// <summary>
        /// Lets sort by name first then lets sort topics greater than queues
        /// </summary>
        /// <param name="that">another destination to compare against</param>
        /// <returns>1 if this is less than o else 0 if they are equal or -1 if this is less than o</returns>
        public int CompareTo(Destination that)
        {
            int answer = 0;
            if(physicalName != that.physicalName)
            {
                if(physicalName == null)
                {
                    return -1;
                }
                else if(that.physicalName == null)
                {
                    return 1;
                }
                answer = physicalName.CompareTo(that.physicalName);
            }

            if(answer == 0)
            {
                if(IsTopic)
                {
                    if(that.IsQueue)
                    {
                        return 1;
                    }
                }
                else
                {
                    if(that.IsTopic)
                    {
                        return -1;
                    }
                }
            }
            return answer;
        }

        /// <summary>
        /// </summary>
        /// <returns>Returns the Destination type</returns>
        public abstract int GetDestinationType();

        public String PhysicalName
        {
            get { return this.physicalName; }
            set { this.physicalName = value; }
        }

        /// <summary>
        /// Returns true if this destination represents a collection of
        /// destinations; allowing a set of destinations to be published to or subscribed
        /// from in one NMS operation.
        /// </summary>
        public bool IsComposite
        {
            get { return physicalName.IndexOf(COMPOSITE_SEPARATOR) > 0; }
        }

        /// <summary>
        /// </summary>
        /// <returns>string representation of this instance</returns>
        public override String ToString()
        {
            switch(DestinationType)
            {
            case DestinationType.Topic:
            return "topic://" + PhysicalName;

            case DestinationType.TemporaryTopic:
            return "temp-topic://" + PhysicalName;

            case DestinationType.TemporaryQueue:
            return "temp-queue://" + PhysicalName;

            default:
            return "queue://" + PhysicalName;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>hashCode for this instance</returns>
        public override int GetHashCode()
        {
            int answer = 37;

            if(this.physicalName != null)
            {
                answer = physicalName.GetHashCode();
            }
            if(IsTopic)
            {
                answer ^= 0xfabfab;
            }
            return answer;
        }

        /// <summary>
        /// if the object passed in is equivalent, return true
        /// </summary>
        /// <param name="obj">the object to compare</param>
        /// <returns>true if this instance and obj are equivalent</returns>
        public override bool Equals(Object obj)
        {
            bool result = this == obj;
            if(!result && obj != null && obj is Destination)
            {
                Destination other = (Destination) obj;
                result = this.GetDestinationType() == other.GetDestinationType()
                    && this.physicalName.Equals(other.physicalName);
            }
            return result;
        }

        /// <summary>
        /// Factory method to create a child destination if this destination is a composite
        /// </summary>
        /// <param name="name"></param>
        /// <returns>the created Destination</returns>
        public abstract Destination CreateDestination(String name);

        public abstract DestinationType DestinationType
        {
            get;
        }

        public override Object Clone()
        {
            // Since we are a derived class use the base's Clone()
            // to perform the shallow copy. Since it is shallow it
            // will include our derived class. Since we are derived,
            // this method is an override.
            Destination o = (Destination) base.Clone();

            // Now do the deep work required
            // If any new variables are added then this routine will
            // likely need updating

            return o;
        }
    }
}

