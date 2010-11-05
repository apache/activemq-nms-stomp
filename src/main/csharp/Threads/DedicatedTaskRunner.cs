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

namespace Apache.NMS.Stomp.Threads
{
    /// <summary>
    /// A TaskRunner that dedicates a single thread to running a single Task.
    /// </summary>
    public class DedicatedTaskRunner : TaskRunner
    {
        private readonly Mutex mutex = new Mutex();
        private readonly AutoResetEvent waiter = new AutoResetEvent(false);
        private readonly ManualResetEvent isShutdown = new ManualResetEvent(true);
        private readonly Thread theThread;
        private readonly Task task;

        private bool terminated;
        private bool pending;
        private bool shutdown;

        public DedicatedTaskRunner(Task task)
        {
            if(task == null)
            {
                throw new NullReferenceException("Task was null");
            }

            this.task = task;

            this.theThread = new Thread(Run);
            this.theThread.IsBackground = true;
            this.theThread.Start();
        }

        public void Shutdown(TimeSpan timeout)
        {
            Monitor.Enter(this.mutex);

            this.shutdown = true;
            this.pending = true;

            this.waiter.Set();

            // Wait till the thread stops ( no need to wait if shutdown
            // is called from thread that is shutting down)
            if(Thread.CurrentThread != this.theThread && !this.terminated)
            {
                Monitor.Exit(this.mutex);
                this.isShutdown.WaitOne((int)timeout.Milliseconds, false);
            }
            else
            {
                Monitor.Exit(this.mutex);
            }
        }

        public void Shutdown()
        {
            Monitor.Enter(this.mutex);

            this.shutdown = true;
            this.pending = true;

            this.waiter.Set();

            // Wait till the thread stops ( no need to wait if shutdown
            // is called from thread that is shutting down)
            if(Thread.CurrentThread != this.theThread && !this.terminated)
            {
                Monitor.Exit(this.mutex);
                this.isShutdown.WaitOne();
            }
            else
            {
                Monitor.Exit(this.mutex);
            }
        }

        public void Wakeup()
        {
            lock(mutex)
            {
                if(this.shutdown)
                {
                    return;
                }

                this.pending = true;

                this.waiter.Set();
            }
        }

        internal void Run()
        {
            lock(this.mutex)
            {
                this.isShutdown.Reset();
            }

            try
            {
                while(true)
                {
                    lock(this.mutex)
                    {
                        pending = false;

                        if(this.shutdown)
                        {
                            return;
                        }
                    }

                    if(!this.task.Iterate())
                    {
                        // wait to be notified.
                        Monitor.Enter(this.mutex);
                        if(this.shutdown)
                        {
                            return;
                        }

                        while(!this.pending)
                        {
                            Monitor.Exit(this.mutex);
                            this.waiter.WaitOne();
                            Monitor.Enter(this.mutex);
                        }
                        Monitor.Exit(this.mutex);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                // Make sure we notify any waiting threads that thread
                // has terminated.
                Monitor.Enter(this.mutex);
                this.terminated = true;
                Monitor.Exit(this.mutex);
                this.isShutdown.Set();
            }
        }
    }
}
