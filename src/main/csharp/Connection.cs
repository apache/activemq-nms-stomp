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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using Apache.NMS.Stomp.Commands;
using Apache.NMS.Stomp.Threads;
using Apache.NMS.Stomp.Transport;
using Apache.NMS.Stomp.Util;
using Apache.NMS.Util;

namespace Apache.NMS.Stomp
{
    /// <summary>
    /// Represents a connection with a message broker
    /// </summary>
    public class Connection : IConnection
    {
        private static readonly IdGenerator CONNECTION_ID_GENERATOR = new IdGenerator();

        private readonly Uri brokerUri;
        private ITransport transport;
        private readonly ConnectionInfo info;

        private AcknowledgementMode acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
        private bool asyncSend = false;
        private bool alwaysSyncSend = false;
        private bool copyMessageOnSend = true;
        private bool sendAcksAsync = false;
        private bool dispatchAsync = true;
        private string transformation = null;
        private IRedeliveryPolicy redeliveryPolicy;
        private PrefetchPolicy prefetchPolicy = new PrefetchPolicy();

        private bool userSpecifiedClientID;
        private TimeSpan requestTimeout;
        private readonly IList sessions = ArrayList.Synchronized(new ArrayList());
        private readonly IDictionary dispatchers = Hashtable.Synchronized(new Hashtable());
        private readonly object myLock = new object();
        private readonly Atomic<bool> connected = new Atomic<bool>(false);
        private readonly Atomic<bool> closed = new Atomic<bool>(false);
        private readonly Atomic<bool> closing = new Atomic<bool>(false);
        private readonly Atomic<bool> transportFailed = new Atomic<bool>(false);
        private Exception firstFailureError = null;
        private int sessionCounter = 0;
        private int temporaryDestinationCounter = 0;
        private int localTransactionCounter;
        private readonly Atomic<bool> started = new Atomic<bool>(false);
        private ConnectionMetaData metaData = null;
        private bool disposed = false;
        private readonly IdGenerator clientIdGenerator;
        private CountDownLatch transportInterruptionProcessingComplete;
        private readonly MessageTransformation messageTransformation;
        private readonly ThreadPoolExecutor executor = new ThreadPoolExecutor();

        public Connection(Uri connectionUri, ITransport transport, IdGenerator clientIdGenerator)
        {
            this.brokerUri = connectionUri;
            this.clientIdGenerator = clientIdGenerator;

			SetTransport(transport);

            ConnectionId id = new ConnectionId();
            id.Value = CONNECTION_ID_GENERATOR.GenerateId();

            this.info = new ConnectionInfo();
            this.info.ConnectionId = id;
            this.info.Host = brokerUri.Host;

            this.messageTransformation = new StompMessageTransformation(this);
        }

        ~Connection()
        {
            Dispose(false);
        }

        /// <summary>
        /// A delegate that can receive transport level exceptions.
        /// </summary>
        public event ExceptionListener ExceptionListener;

        /// <summary>
        /// An asynchronous listener that is notified when a Fault tolerant connection
        /// has been interrupted.
        /// </summary>
        public event ConnectionInterruptedListener ConnectionInterruptedListener;

        /// <summary>
        /// An asynchronous listener that is notified when a Fault tolerant connection
        /// has been resumed.
        /// </summary>
        public event ConnectionResumedListener ConnectionResumedListener;

        #region Properties

        public String UserName
        {
            get { return this.info.UserName; }
            set { this.info.UserName = value; }
        }

        public String Password
        {
            get { return this.info.Password; }
            set { this.info.Password = value; }
        }

        /// <summary>
        /// This property indicates whether or not async send is enabled.
        /// </summary>
        public bool AsyncSend
        {
            get { return asyncSend; }
            set { asyncSend = value; }
        }

        /// <summary>
        /// This property sets the acknowledgment mode for the connection.
        /// The URI parameter connection.ackmode can be set to a string value
        /// that maps to the enumeration value.
        /// </summary>
        public string AckMode
        {
            set { this.acknowledgementMode = NMSConvert.ToAcknowledgementMode(value); }
        }

        /// <summary>
        /// This property forces all messages that are sent to be sent synchronously overriding
        /// any usage of the AsyncSend flag. This can reduce performance in some cases since the
        /// only messages we normally send synchronously are Persistent messages not sent in a
        /// transaction. This options guarantees that no send will return until the broker has
        /// acknowledge receipt of the message
        /// </summary>
        public bool AlwaysSyncSend
        {
            get { return alwaysSyncSend; }
            set { alwaysSyncSend = value; }
        }

        /// <summary>
        /// This property indicates whether Message's should be copied before being sent via
        /// one of the Connection's send methods.  Copying the Message object allows the user
        /// to resuse the Object over for another send.  If the message isn't copied performance
        /// can improve but the user must not reuse the Object as it may not have been sent
        /// before they reset its payload.
        /// </summary>
        public bool CopyMessageOnSend
        {
            get { return copyMessageOnSend; }
            set { copyMessageOnSend = value; }
        }

        /// <summary>
        /// This property indicates whether or not async sends are used for
        /// message acknowledgement messages.  Sending Acks async can improve
        /// performance but may decrease reliability.
        /// </summary>
        public bool SendAcksAsync
        {
            get { return sendAcksAsync; }
            set { sendAcksAsync = value; }
        }

        /// <summary>
        /// synchronously or asynchronously by the broker.  Set to false for a slow
        /// consumer and true for a fast consumer.
        /// </summary>
        public bool DispatchAsync
        {
            get { return this.dispatchAsync; }
            set { this.dispatchAsync = value; }
        }

        /// <summary>
        /// Sets the default Transformation attribute applied to Consumers.  If a consumer
        /// is to receive Map messages from the Broker then the user should set the "jms-map-xml"
        /// transformation on the consumer so that all MapMessages are sent as XML.
        /// </summary>
        public string Transformation
        {
            get { return this.transformation; }
            set { this.transformation = value; }
        }

        public IConnectionMetaData MetaData
        {
            get { return this.metaData ?? (this.metaData = new ConnectionMetaData()); }
        }

        public Uri BrokerUri
        {
            get { return brokerUri; }
        }

        public ITransport ITransport
        {
            get { return transport; }
            set { this.transport = value; }
        }

        public bool TransportFailed
        {
            get { return this.transportFailed.Value; }
        }

        public Exception FirstFailureError
        {
            get { return this.firstFailureError; }
        }

        public TimeSpan RequestTimeout
        {
            get { return this.requestTimeout; }
            set { this.requestTimeout = value; }
        }

        public AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { this.acknowledgementMode = value; }
        }

        public string ClientId
        {
            get { return info.ClientId; }
            set
            {
                if(this.connected.Value)
                {
                    throw new NMSException("You cannot change the ClientId once the Connection is connected");
                }

                this.info.ClientId = value;
                this.userSpecifiedClientID = true;
                CheckConnected();
            }
        }

        /// <summary>
        /// The Default Client Id used if the ClientId property is not set explicity.
        /// </summary>
        public string DefaultClientId
        {
            set
            {
                this.info.ClientId = value;
                this.userSpecifiedClientID = true;
            }
        }

        public ConnectionId ConnectionId
        {
            get { return info.ConnectionId; }
        }

        /// <summary>
        /// Get/or set the redelivery policy for this connection.
        /// </summary>
        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.redeliveryPolicy; }
            set { this.redeliveryPolicy = value; }
        }

        public PrefetchPolicy PrefetchPolicy
        {
            get { return this.prefetchPolicy; }
            set { this.prefetchPolicy = value; }
        }

        internal MessageTransformation MessageTransformation
        {
            get { return this.messageTransformation; }
        }

        private ConsumerTransformerDelegate consumerTransformer;
        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumerTransformer; }
            set { this.consumerTransformer = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.producerTransformer; }
            set { this.producerTransformer = value; }
        }

        #endregion

		private void SetTransport(ITransport newTransport)
		{
			this.transport = newTransport;
			this.transport.Command = new CommandHandler(OnCommand);
			this.transport.Exception = new ExceptionHandler(OnTransportException);
			this.transport.Interrupted = new InterruptedHandler(OnTransportInterrupted);
			this.transport.Resumed = new ResumedHandler(OnTransportResumed);
		}

		/// <summary>
        /// Starts asynchronous message delivery of incoming messages for this connection.
        /// Synchronous delivery is unaffected.
        /// </summary>
        public void Start()
        {
            CheckConnected();
            if(started.CompareAndSet(false, true))
            {
                lock(sessions.SyncRoot)
                {
                    foreach(Session session in sessions)
                    {
                        session.Start();
                    }
                }
            }
        }

        /// <summary>
        /// This property determines if the asynchronous message delivery of incoming
        /// messages has been started for this connection.
        /// </summary>
        public bool IsStarted
        {
            get { return started.Value; }
        }

        /// <summary>
        /// Temporarily stop asynchronous delivery of inbound messages for this connection.
        /// The sending of outbound messages is unaffected.
        /// </summary>
        public void Stop()
        {
            CheckConnected();
            if(started.CompareAndSet(true, false))
            {
                lock(sessions.SyncRoot)
                {
                    foreach(Session session in sessions)
                    {
                        session.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
        public ISession CreateSession()
        {
            return CreateSession(acknowledgementMode);
        }

        /// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
        public ISession CreateSession(AcknowledgementMode sessionAcknowledgementMode)
        {
            SessionInfo info = CreateSessionInfo(sessionAcknowledgementMode);
            Session session = new Session(this, info, sessionAcknowledgementMode, this.dispatchAsync);

            // Set properties on session using parameters prefixed with "session."
            if(!String.IsNullOrEmpty(brokerUri.Query) && !brokerUri.OriginalString.EndsWith(")"))
            {
                // Since the Uri class will return the end of a Query string found in a Composite
                // URI we must ensure that we trim that off before we proceed.
                string query = brokerUri.Query.Substring(brokerUri.Query.LastIndexOf(")") + 1);
                StringDictionary options = URISupport.ParseQuery(query);
                options = URISupport.GetProperties(options, "session.");
                URISupport.SetProperties(session, options);
            }

            session.ConsumerTransformer = this.ConsumerTransformer;
            session.ProducerTransformer = this.ProducerTransformer;

            if(IsStarted)
            {
                session.Start();
            }

            sessions.Add(session);
            return session;
        }

        internal void RemoveSession(Session session)
        {
            if(!this.closing.Value)
            {
                sessions.Remove(session);
            }
        }

        internal void addDispatcher(ConsumerId id, IDispatcher dispatcher)
        {
            this.dispatchers.Add(id, dispatcher);
        }

        internal void removeDispatcher(ConsumerId id)
        {
            this.dispatchers.Remove(id);
        }

        public void Close()
        {
            lock(myLock)
            {
                if(this.closed.Value)
                {
                    return;
                }

                try
                {
                    Tracer.Info("Closing Connection.");
                    this.closing.Value = true;
                    lock(sessions.SyncRoot)
                    {
                        foreach(Session session in sessions)
                        {
                            session.DoClose();
                        }
                    }
                    sessions.Clear();

                    if(connected.Value)
                    {
                        ShutdownInfo shutdowninfo = new ShutdownInfo();
                        transport.Oneway(shutdowninfo);
                    }

                    Tracer.Info("Disposing of the Transport.");
					transport.Stop();
                    transport.Dispose();
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Error during connection close: {0}", ex.Message);
                }
                finally
                {
                    this.transport = null;
                    this.closed.Value = true;
                    this.connected.Value = false;
                    this.closing.Value = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(disposed)
            {
                return;
            }

            if(disposing)
            {
                // Dispose managed code here.
            }

            try
            {
                // For now we do not distinguish between Dispose() and Close().
                // In theory Dispose should possibly be lighter-weight and perform a (faster)
                // disorderly close.
                Close();
            }
            catch
            {
                // Ignore network errors.
            }

            disposed = true;
        }

        public void PurgeTempDestinations()
        {
        }

        // Implementation methods

        /// <summary>
        /// Performs a synchronous request-response with the broker
        /// </summary>
        ///

        public Response SyncRequest(Command command)
        {
            return SyncRequest(command, this.RequestTimeout);
        }

        public Response SyncRequest(Command command, TimeSpan requestTimeout)
        {
            CheckConnected();

            try
            {
                Response response = transport.Request(command, requestTimeout);
                if(response is ExceptionResponse)
                {
                    ExceptionResponse exceptionResponse = (ExceptionResponse) response;
                    BrokerError brokerError = exceptionResponse.Exception;
                    throw new BrokerException(brokerError);
                }
                return response;
            }
            catch(Exception ex)
            {
                throw NMSExceptionSupport.Create(ex);
            }
        }

        public void Oneway(Command command)
        {
            CheckConnected();

            try
            {
                transport.Oneway(command);
            }
            catch(Exception ex)
            {
                throw NMSExceptionSupport.Create(ex);
            }
        }

        private object connectedLock = new object();

        /// <summary>
        /// Check and ensure that the connection objcet is connected.  If it is not
        /// connected or is closed, a ConnectionClosedException is thrown.
        /// </summary>
        internal void CheckConnected()
        {
            if(closed.Value)
            {
                throw new ConnectionClosedException();
            }

            if(!connected.Value)
            {
                DateTime timeoutTime = DateTime.Now + this.RequestTimeout;
                int waitCount = 1;

                while(true)
                {
                    if(Monitor.TryEnter(connectedLock))
                    {
                        try
                        {
                            if(closed.Value || closing.Value)
                            {
                                break;
                            }
                            else if(!connected.Value)
                            {
                                if(!this.userSpecifiedClientID)
                                {
                                    this.info.ClientId = this.clientIdGenerator.GenerateId();
                                }

                                try
                                {
                                    if(null != transport)
                                    {
										// Make sure the transport is started.
										if(!this.transport.IsStarted)
										{
											this.transport.Start();
										}
										
										// Send the connection and see if an ack/nak is returned.
                                        Response response = transport.Request(this.info, this.RequestTimeout);
                                        if(!(response is ExceptionResponse))
                                        {
                                            connected.Value = true;
                                        }
										else
										{
											ExceptionResponse error = response as ExceptionResponse;
											NMSException exception = CreateExceptionFromBrokerError(error.Exception);
											// This is non-recoverable.
											// Shutdown the transport connection, and re-create it, but don't start it.
											// It will be started if the connection is re-attempted.
											this.transport.Stop();
											ITransport newTransport = TransportFactory.CreateTransport(this.brokerUri);
											SetTransport(newTransport);
											throw exception;
										}
									}
                                }
                                catch
                                {
                                }
                            }
                        }
                        finally
                        {
                            Monitor.Exit(connectedLock);
                        }
                    }

                    if(connected.Value || closed.Value || closing.Value || DateTime.Now > timeoutTime)
                    {
                        break;
                    }

                    // Back off from being overly aggressive.  Having too many threads
                    // aggressively trying to connect to a down broker pegs the CPU.
                    Thread.Sleep(5 * (waitCount++));
                }

                if(!connected.Value)
                {
                    throw new ConnectionClosedException();
                }
            }
        }

        /// <summary>
        /// Handle incoming commands
        /// </summary>
        /// <param name="commandTransport">An ITransport</param>
        /// <param name="command">A  Command</param>
        protected void OnCommand(ITransport commandTransport, Command command)
        {
            if(command.IsMessageDispatch)
            {
                // We wait if the Connection is still processing interruption
                // code to reset the MessageConsumers.
                WaitForTransportInterruptionProcessingToComplete();
                DispatchMessage((MessageDispatch) command);
            }
            else if(command.IsWireFormatInfo)
            {
                // Ignore for now, might need to save if off later.
            }
            else if(command.IsKeepAliveInfo)
            {
                // Ignore only the InactivityMonitor cares about this one.
            }
            else if(command.IsErrorCommand)
            {
                if(!closing.Value && !closed.Value)
                {
                    ConnectionError connectionError = (ConnectionError) command;
                    BrokerError brokerError = connectionError.Exception;
                    string message = "Broker connection error.";
                    string cause = "";

                    if(null != brokerError)
                    {
                        message = brokerError.Message;
                        if(null != brokerError.Cause)
                        {
                            cause = brokerError.Cause.Message;
                        }
                    }

                    OnException(new NMSConnectionException(message, cause));
                }
            }
            else
            {
                Tracer.ErrorFormat("Unknown command: {0}", command);
            }
        }

        protected void DispatchMessage(MessageDispatch dispatch)
        {
            lock(dispatchers.SyncRoot)
            {
                if(dispatchers.Contains(dispatch.ConsumerId))
                {
                    IDispatcher dispatcher = (IDispatcher) dispatchers[dispatch.ConsumerId];

                    // Can be null when a consumer has sent a MessagePull and there was
                    // no available message at the broker to dispatch.
                    if(dispatch.Message != null)
                    {
                        dispatch.Message.ReadOnlyBody = true;
                        dispatch.Message.ReadOnlyProperties = true;
                        dispatch.Message.RedeliveryCounter = dispatch.RedeliveryCounter;
                    }

                    dispatcher.Dispatch(dispatch);

                    return;
                }
            }

            Tracer.ErrorFormat("No such consumer active: {0}.", dispatch.ConsumerId);
        }

        protected void OnTransportException(ITransport sender, Exception exception)
        {
            this.OnException(exception);
        }

        internal void OnAsyncException(Exception error)
        {
            if(!this.closed.Value && !this.closing.Value)
            {
                if(this.ExceptionListener != null)
                {
                    if(!(error is NMSException))
                    {
                        error = NMSExceptionSupport.Create(error);
                    }
                    NMSException e = (NMSException)error;

                    // Called in another thread so that processing can continue
                    // here, ensures no lock contention.
                    executor.QueueUserWorkItem(AsyncCallExceptionListener, e);
                }
                else
                {
                    Tracer.DebugFormat("Async exception with no exception listener: {0}", error.Message);
                }
            }
        }

        private void AsyncCallExceptionListener(object error)
        {
            NMSException exception = error as NMSException;
            this.ExceptionListener(exception);
        }

        internal void OnException(Exception error)
        {
            // Will fire an exception listener callback if there's any set.
            OnAsyncException(error);

            if(!this.closing.Value && !this.closed.Value)
            {
                // Perform the actual work in another thread to avoid lock contention
                // and allow the caller to continue on in its error cleanup.
                executor.QueueUserWorkItem(AsyncOnExceptionHandler, error);
            }
        }

        private void AsyncOnExceptionHandler(object error)
        {
            Exception cause = error as Exception;

            MarkTransportFailed(cause);

            try
            {
                this.transport.Dispose();
            }
            catch(Exception ex)
            {
                Tracer.DebugFormat("Caught Exception While disposing of Transport: {0}", ex.Message);
            }

            IList sessionsCopy = null;
            lock(this.sessions.SyncRoot)
            {
                sessionsCopy = new ArrayList(this.sessions);
            }

            // Use a copy so we don't concurrently modify the Sessions list if the
            // client is closing at the same time.
            foreach(Session session in sessionsCopy)
            {
                try
                {
                    session.Dispose();
                }
                catch(Exception ex)
                {
                    Tracer.DebugFormat("Caught Exception While disposing of Sessions: {0}", ex.Message);
                }
            }
        }

        protected void OnTransportInterrupted(ITransport sender)
        {
            Tracer.Debug("Transport has been Interrupted.");

            this.transportInterruptionProcessingComplete = new CountDownLatch(dispatchers.Count);
            if(Tracer.IsDebugEnabled)
            {
                Tracer.DebugFormat("transport interrupted, dispatchers: {0}", dispatchers.Count);
            }

            foreach(Session session in this.sessions)
            {
                session.ClearMessagesInProgress();
            }

            if(this.ConnectionInterruptedListener != null && !this.closing.Value)
            {
                try
                {
                    this.ConnectionInterruptedListener();
                }
                catch
                {
                }
            }
        }

        protected void OnTransportResumed(ITransport sender)
        {
            Tracer.Debug("Transport has resumed normal operation.");

            if(this.ConnectionResumedListener != null && !this.closing.Value)
            {
                try
                {
                    this.ConnectionResumedListener();
                }
                catch
                {
                }
            }
        }

        internal void OnSessionException(Session sender, Exception exception)
        {
            if(ExceptionListener != null)
            {
                try
                {
                    ExceptionListener(exception);
                }
                catch
                {
                    sender.Close();
                }
            }
        }

        private void MarkTransportFailed(Exception error)
        {
            this.transportFailed.Value = true;
            if(this.firstFailureError == null)
            {
                this.firstFailureError = error;
            }
        }

        /// <summary>
        /// Creates a new temporary destination name
        /// </summary>
        public String CreateTemporaryDestinationName()
        {
            return info.ConnectionId.Value + ":" + Interlocked.Increment(ref temporaryDestinationCounter);
        }

        /// <summary>
        /// Creates a new local transaction ID
        /// </summary>
        public TransactionId CreateLocalTransactionId()
        {
            TransactionId id = new TransactionId();
            id.ConnectionId = ConnectionId;
            id.Value = Interlocked.Increment(ref localTransactionCounter);
            return id;
        }

        protected SessionInfo CreateSessionInfo(AcknowledgementMode sessionAcknowledgementMode)
        {
            SessionInfo answer = new SessionInfo();
            SessionId sessionId = new SessionId();
            sessionId.ConnectionId = info.ConnectionId.Value;
            sessionId.Value = Interlocked.Increment(ref sessionCounter);
            answer.SessionId = sessionId;
            return answer;
        }

        private void WaitForTransportInterruptionProcessingToComplete()
        {
            CountDownLatch cdl = this.transportInterruptionProcessingComplete;
            if(cdl != null)
            {
                if(!closed.Value && cdl.Remaining > 0)
                {
                    Tracer.WarnFormat("dispatch paused, waiting for outstanding dispatch interruption " +
								"processing ({0}) to complete..", cdl.Remaining);
                    cdl.await(TimeSpan.FromSeconds(10));
                }
            }
        }

        internal void TransportInterruptionProcessingComplete()
        {
            CountDownLatch cdl = this.transportInterruptionProcessingComplete;
            if(cdl != null)
            {
                cdl.countDown();
            }
        }

		private NMSException CreateExceptionFromBrokerError(BrokerError brokerError)
		{
			String exceptionClassName = brokerError.ExceptionClass;

			if(String.IsNullOrEmpty(exceptionClassName))
			{
				return new BrokerException(brokerError);
			}

			return new InvalidClientIDException(brokerError.Message);
		}
	}
}
