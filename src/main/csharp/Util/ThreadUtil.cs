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

namespace Apache.NMS.Stomp.Util
{
    public class ThreadUtil
    {
       public static bool MonitorWait(Mutex mutex, int timeout)
       {
#if NETCF
            int waitTime = 0;
            bool acquiredLock = false;

            // Release so that the reconnect task can run
            Monitor.Exit(mutex);
            // Wait for something
            while(!(acquiredLock = Monitor.TryEnter(mutex)) && waitTime < timeout)
            {
                Thread.Sleep(1);
                waitTime++;
            }

            return acquiredLock;
#else
           return Monitor.Wait(mutex, timeout);
#endif
       }

       public static void DisposeTimer(Timer timer, int timeout)
       {
#if NETCF
            timer.Dispose();
#else
            AutoResetEvent shutdownEvent = new AutoResetEvent(false);

            // Attempt to wait for the Timer to shutdown
            timer.Dispose(shutdownEvent);
            shutdownEvent.WaitOne(timeout, false);
#endif
       }
    }
}
