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
using System.Collections.Generic;
using System.Threading;

namespace Apache.NMS.Stomp.Threads
{
    /// <summary>
    /// A TaskRunner that dedicates a single thread to running a single Task.
    /// </summary>
    public class CompositeTaskRunner : TaskRunner
    {
        private readonly Mutex mutex = new Mutex();
        private readonly AutoResetEvent waiter = new AutoResetEvent(false);
        private readonly ManualResetEvent isShutdown = new ManualResetEvent(true);

        private readonly Thread theThread = null;
        private readonly LinkedList<CompositeTask> tasks = new LinkedList<CompositeTask>();

        private bool terminated = false;
        private bool pending = false;
        private bool shutdown = false;
        
        public CompositeTaskRunner()
        {
            this.theThread = new Thread(Run) {IsBackground = true};
            this.theThread.Start();
        }
		
		public void AddTask(CompositeTask task)
		{
			lock(mutex)
			{
				this.tasks.AddLast(task);
				this.Wakeup();
			}
		}

		public void RemoveTask(CompositeTask task)
		{
			lock(mutex)
			{
				this.tasks.Remove(task);
				this.Wakeup();
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

                    if(!this.Iterate())
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
		
		private bool Iterate()
		{
			lock(mutex)
			{
				foreach(CompositeTask task in this.tasks)
				{
					if(task.IsPending)
					{
						task.Iterate();

						// Always return true here so that we can check the next
						// task in the list to see if its done.
						return true;
					}
				}
			}
			
			return false;
		}
    }
}
