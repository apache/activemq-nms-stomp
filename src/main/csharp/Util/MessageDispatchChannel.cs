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
using System.Threading;
using System.Collections.Generic;
using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp.Util
{
    public class MessageDispatchChannel
    {
        private readonly Mutex mutex = new Mutex();
        private readonly ManualResetEvent waiter = new ManualResetEvent(false);
        private bool closed;
        private bool running;
        private readonly LinkedList<MessageDispatch> channel = new LinkedList<MessageDispatch>();

        public MessageDispatchChannel()
        {
        }

        #region Properties

        public object SyncRoot
        {
            get{ return this.mutex; }
        }

        public bool Closed
        {
            get
            {
                lock(this.mutex)
                {
                    return this.closed;
                }
            }

            set
            {
                lock(this.mutex)
                {
                    this.closed = value;
                }
            }
        }

        public bool Running
        {
            get
            {
                lock(this.mutex)
                {
                    return this.running;
                }
            }

            set
            {
                lock(this.mutex)
                {
                    this.running = value;
                }
            }
        }

        public bool Empty
        {
            get
            {
                lock(mutex)
                {
                    return channel.Count == 0;
                }
            }
        }

        public long Count
        {
            get
            {
                lock(mutex)
                {
                    return channel.Count;
                }
            }
        }

        #endregion

        public void Start()
        {
            lock(this.mutex)
            {
                if(!Closed)
                {
                    this.running = true;
                    this.waiter.Reset();
                }
            }
        }

        public void Stop()
        {
            lock(mutex)
            {
                this.running = false;
                this.waiter.Set();
            }
        }

        public void Close()
        {
            lock(mutex)
            {
                if(!Closed)
                {
                    this.running = false;
                    this.closed = true;
                }

                this.waiter.Set();
            }
        }

        public void Enqueue(MessageDispatch dispatch)
        {
            lock(this.mutex)
            {
                this.channel.AddLast(dispatch);
                this.waiter.Set();
            }
        }

        public void EnqueueFirst(MessageDispatch dispatch)
        {
            lock(this.mutex)
            {
                this.channel.AddFirst(dispatch);
                this.waiter.Set();
            }
        }

        public MessageDispatch Dequeue(TimeSpan timeout)
        {
            MessageDispatch result = null;

            this.mutex.WaitOne();

            // Wait until the channel is ready to deliver messages.
            if( timeout != TimeSpan.Zero && !Closed && ( Empty || !Running ) )
            {
                // This isn't the greatest way to do this but to work on the
                // .NETCF its the only solution I could find so far.  This
                // code will only really work for one Thread using the event
                // channel to wait as all waiters are going to drop out of
                // here regardless of the fact that only one message could
                // be on the Queue.  
                this.waiter.Reset();
                this.mutex.ReleaseMutex();
                this.waiter.WaitOne((int)timeout.TotalMilliseconds, false);
                this.mutex.WaitOne();
            }

            if( !Closed && Running && !Empty )
            {
                result = DequeueNoWait();
            }

            this.mutex.ReleaseMutex();

            return result;
        }

        public MessageDispatch DequeueNoWait()
        {
            MessageDispatch result = null;

            lock(this.mutex)
            {
                if( Closed || !Running || Empty )
                {
                    return null;
                }

                result = channel.First.Value;
                this.channel.RemoveFirst();
            }

            return result;
        }

        public MessageDispatch Peek()
        {
            lock(this.mutex)
            {
                if( Closed || !Running || Empty )
                {
                    return null;
                }

                return channel.First.Value;
            }
        }

        public void Clear()
        {
            lock(mutex)
            {
                this.channel.Clear();
            }
        }

        public MessageDispatch[] RemoveAll()
        {
            MessageDispatch[] result;

            lock(mutex)
            {
                result = new MessageDispatch[this.Count];
                channel.CopyTo(result, 0);
                channel.Clear();
            }

            return result;
        }
    }
}
