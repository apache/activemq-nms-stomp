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

using System.Threading;

namespace Apache.NMS.Stomp.Util
{
    public class ThreadUtil
    {
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

       public static void WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
       {
#if NETCF
           // TODO: Implement .NET CF version of WaitAny().
#else
           WaitHandle.WaitAny(waitHandles, millisecondsTimeout, exitContext);
#endif
       }
    }
}
