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
using Apache.NMS.Stomp.Commands;
using Apache.NMS.Stomp.Threads;
using Apache.NMS.Stomp.Util;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp.Transport
{
    /// <summary>
    /// This class make sure that the connection is still alive,
    /// by monitoring the reception of commands from the peer of
    /// the transport.
    /// </summary>
    public class InactivityMonitor : TransportFilter
    {
        private readonly Atomic<bool> monitorStarted = new Atomic<bool>(false);

        private readonly Atomic<bool> commandSent = new Atomic<bool>(false);
        private readonly Atomic<bool> commandReceived = new Atomic<bool>(false);

        private readonly Atomic<bool> failed = new Atomic<bool>(false);
        private readonly Atomic<bool> inRead = new Atomic<bool>(false);
        private readonly Atomic<bool> inWrite = new Atomic<bool>(false);

        private DedicatedTaskRunner asyncTask;
        private AsyncWriteTask asyncWriteTask;

        private readonly Mutex monitor = new Mutex();

        private Timer connectionCheckTimer;

        private long maxInactivityDuration = 10000;
        public long MaxInactivityDuration
        {
            get { return this.maxInactivityDuration; }
            set { this.maxInactivityDuration = value; }
        }

        private long maxInactivityDurationInitialDelay = 10000;
        public long MaxInactivityDurationInitialDelay
        {
            get { return this.maxInactivityDurationInitialDelay; }
            set { this.maxInactivityDurationInitialDelay = value; }
        }

        /// <summary>
        /// Constructor or the Inactivity Monitor
        /// </summary>
        /// <param name="next"></param>
        public InactivityMonitor(ITransport next)
            : base(next)
        {
            Tracer.Debug("Creating Inactivity Monitor");
        }

        ~InactivityMonitor()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                // get rid of unmanaged stuff
            }

            StopMonitorThreads();

            base.Dispose(disposing);
        }

        #region WriteCheck Related
        /// <summary>
        /// Check the write to the broker
        /// </summary>
        public void WriteCheck(object unused)
        {
            if(this.inWrite.Value || this.failed.Value)
            {
                Tracer.Debug("Inactivity Monitor is in write or already failed.");
                return;
            }

            if(!commandSent.Value)
            {
                Tracer.Debug("No Message sent since last write check. Sending a KeepAliveInfo");
                this.asyncTask.Wakeup();
            }
            else
            {
                Tracer.Debug("Message sent since last write check. Resetting flag");
            }

            commandSent.Value = false;
        }
        #endregion

        public override void Stop()
        {
            StopMonitorThreads();
            next.Stop();
        }

        protected override void OnCommand(ITransport sender, Command command)
        {
            commandReceived.Value = true;
            inRead.Value = true;
            try
            {
                try
                {
                    StartMonitorThreads();
                }
                catch(IOException ex)
                {
                    OnException(this, ex);
                }

                base.OnCommand(sender, command);
            }
            finally
            {
                inRead.Value = false;
            }
        }

        public override void Oneway(Command command)
        {
            // Disable inactivity monitoring while processing a command.
            // synchronize this method - its not synchronized
            // further down the transport stack and gets called by more
            // than one thread  by this class
            lock(inWrite)
            {
                inWrite.Value = true;
                try
                {
                    if(failed.Value)
                    {
                        throw new IOException("Channel was inactive for too long: " + next.RemoteAddress.ToString());
                    }

                    next.Oneway(command);
                }
                finally
                {
                    commandSent.Value = true;
                    inWrite.Value = false;
                }
            }
        }

        protected override void OnException(ITransport sender, Exception command)
        {
            if(failed.CompareAndSet(false, true))
            {
                Tracer.Debug("Exception received in the Inactivity Monitor: " + command.ToString());
                StopMonitorThreads();
                base.OnException(sender, command);
            }
        }

        private void StartMonitorThreads()
        {
            lock(monitor)
            {
                if(monitorStarted.Value || maxInactivityDuration == 0)
                {
                    return;
                }

                Tracer.DebugFormat("Inactivity: Write Check time interval: {0}", maxInactivityDuration );
				Tracer.DebugFormat("Inactivity: Initial Delay time interval: {0}", maxInactivityDurationInitialDelay );

                this.asyncWriteTask = new AsyncWriteTask(this);
                this.asyncTask = new DedicatedTaskRunner(this.asyncWriteTask);

                monitorStarted.Value = true;

                this.connectionCheckTimer = new Timer(
                    new TimerCallback(WriteCheck),
                    null,
                    maxInactivityDurationInitialDelay,
                    maxInactivityDuration
                    );
            }
        }

        private void StopMonitorThreads()
        {
            lock(monitor)
            {
                if(monitorStarted.CompareAndSet(true, false))
                {
                    // Attempt to wait for the Timer to shutdown, but don't wait
                    // forever, if they don't shutdown after two seconds, just quit.
                    ThreadUtil.DisposeTimer(connectionCheckTimer, 2000);

                    this.asyncTask.Shutdown();
                    this.asyncTask = null;
                    this.asyncWriteTask = null;
                }
            }
        }

        #region Async Tasks

        // Task that fires when the TaskRunner is signaled by the WriteCheck Timer Task.
        class AsyncWriteTask : Task
        {
            private readonly InactivityMonitor parent;

            public AsyncWriteTask(InactivityMonitor parent)
            {
                this.parent = parent;
            }

            public bool Iterate()
            {
				Tracer.Debug("AsyncWriteTask perparing for another Write Check");
                if(this.parent.monitorStarted.Value)
                {
                    try
                    {
						Tracer.Debug("AsyncWriteTask Write Check required sending KeepAlive.");
                        KeepAliveInfo info = new KeepAliveInfo();
                        this.parent.Oneway(info);
                    }
                    catch(IOException e)
                    {
                        this.parent.OnException(parent, e);
                    }
                }

                return false;
            }
        }
        #endregion
    }

}
