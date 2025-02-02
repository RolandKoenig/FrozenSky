﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
	Exception are projects where it is noted otherwhise.
    More info at 
     - https://github.com/RolandKoenig/SeeingSharp (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
    Copyright (C) 2016 Roland König (RolandK)
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/
#endregion
using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SeeingSharp.Checking;

namespace SeeingSharp.Util
{
    /// <summary>
    /// Main class for sending/receiving messages.
    /// This class followes the Messenger pattern but modifies it on some parts like 
    /// thread synchronization.
    /// What 'messenger' actually is, see here a short explanation: http://stackoverflow.com/questions/22747954/mvvm-light-toolkit-messenger-uses-event-aggregator-or-mediator-pattern
    /// </summary>
    public class SeeingSharpMessenger
    {
        /// <summary>
        /// Gets or sets a custom exception handler which is used globally.
        /// Return true to skip default exception behavior (exception is thrown to publisher by default).
        /// </summary>
        public static Func<SeeingSharpMessenger, Exception, bool> CustomPublishExceptionHandler;

        // Global synchronization (enables the possibility to publish a message over more threads / areas of the application)
        private static ConcurrentDictionary<string, SeeingSharpMessenger> s_messengersByName;

        // Global information about message routing
        #region
        private static ConcurrentDictionary<Type, string[]> s_messagesAsyncTargets;
        private static ConcurrentDictionary<Type, string[]> s_messageSources;
        #endregion

        // Checking and global synchronization
        #region
        private string m_messengerName;
        private SynchronizationContext m_syncContext;
        private SeeingSharpMessageThreadingBehavior m_checkBehavior;
        #endregion

        // Message subscriptions
        #region
        private Dictionary<Type, List<MessageSubscription>> m_messageSubscriptions;
        private object m_messageSubscriptionsLock;
        #endregion

        /// <summary>
        /// Initializes the <see cref="SeeingSharpMessenger" /> class.
        /// </summary>
        static SeeingSharpMessenger()
        {
            s_messengersByName = new ConcurrentDictionary<string, SeeingSharpMessenger>();

            s_messagesAsyncTargets = new ConcurrentDictionary<Type, string[]>();
            s_messageSources = new ConcurrentDictionary<Type, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeeingSharpMessenger"/> class.
        /// </summary>
        public SeeingSharpMessenger()
        {
            m_messengerName = string.Empty;
            m_syncContext = null;
            m_checkBehavior = SeeingSharpMessageThreadingBehavior.Ignore;

            m_messageSubscriptions = new Dictionary<Type, List<MessageSubscription>>();
            m_messageSubscriptionsLock = new object();
        }

        /// <summary>
        /// Gets the SeeingSharpMessenger by the given name.
        /// </summary>
        /// <param name="messengerName">The name of the messenger.</param>
        public static SeeingSharpMessenger GetByName(string messengerName)
        {
            messengerName.EnsureNotNullOrEmpty(nameof(messengerName));

            var result = TryGetByName(messengerName);
            if (result == null) { throw new SeeingSharpException(string.Format("Unable to find Messenger for thread {0}!", messengerName)); }
            return result;
        }

        /// <summary>
        /// Gets the SeeingSharpMessenger by the given name.
        /// </summary>
        /// <param name="messengerName">The name of the messenger.</param>
        public static SeeingSharpMessenger TryGetByName(string messengerName)
        {
            messengerName.EnsureNotNullOrEmpty(nameof(messengerName));

            SeeingSharpMessenger result = null;
            s_messengersByName.TryGetValue(messengerName, out result);
            return result;
        }

        /// <summary>
        /// Sets all required threading properties based on the given target thread.
        /// The name of the messenger is directly taken from the given ObjectThread.
        /// </summary>
        /// <param name="targetThread">The thread on which this Messanger should work on.</param>
        public void ApplyForGlobalSynchronization(ObjectThread targetThread)
        {
            targetThread.EnsureNotNull(nameof(targetThread));

            ApplyForGlobalSynchronization(
                SeeingSharpMessageThreadingBehavior.EnsureMainSyncContextOnSyncCalls,
                targetThread.Name,
                targetThread.SyncContext);
        }

        /// <summary>
        /// Sets all required synchronization properties.
        /// </summary>
        /// <param name="checkBehavior">Defines the checking behavior for Publish calls.</param>
        /// <param name="messengerName">The name by which this messenger should be registered.</param>
        /// <param name="syncContext">The synchronization context to be used.</param>
        public void ApplyForGlobalSynchronization(SeeingSharpMessageThreadingBehavior checkBehavior, string messengerName, SynchronizationContext syncContext)
        {
            messengerName.EnsureNotNullOrEmpty(nameof(messengerName));
            syncContext.EnsureNotNull(nameof(syncContext));

            m_messengerName = messengerName;
            m_checkBehavior = checkBehavior;
            m_syncContext = syncContext;

            if (!string.IsNullOrEmpty(messengerName))
            {
                s_messengersByName.TryAdd(messengerName, this);
            }
        }

        /// <summary>
        /// Clears all synchronization options.
        /// </summary>
        public void DiscardGlobalSynchronization()
        {
            if (string.IsNullOrEmpty(m_messengerName)) { return; }

            SeeingSharpMessenger dummyResult = null;
            s_messengersByName.TryRemove(m_messengerName, out dummyResult);

            m_checkBehavior = SeeingSharpMessageThreadingBehavior.Ignore;
            m_messengerName = string.Empty;
            m_syncContext = null;
        }

        /// <summary>
        /// Gets a collection containing all active subscriptions.
        /// </summary>
        public List<MessageSubscription> GetAllSubscriptions()
        {
            List<MessageSubscription> result = new List<MessageSubscription>();

            lock (m_messageSubscriptionsLock)
            {
                foreach (KeyValuePair<Type, List<MessageSubscription>> actPair in m_messageSubscriptions)
                {
                    foreach(var actSubscription in actPair.Value)
                    {
                        result.Add(actSubscription);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Waits for the given message.
        /// </summary>
        public Task<T> WaitForMessageAsync<T>()
            where T : SeeingSharpMessage
        {
            TaskCompletionSource<T> taskComplSource = new TaskCompletionSource<T>();

            MessageSubscription subscription = null;
            subscription = this.Subscribe<T>((message) =>
            {
                // Unsubscribe first
                subscription.Unsubscribe();

                // Set the task's result
                taskComplSource.SetResult(message);
            });

            return taskComplSource.Task;
        }

#if DESKTOP
        /// <summary>
        /// Subscribes all receiver-methods of the given target object to this Messenger.
        /// Subscribe and unsubscribe is automatically called when the control is created/destroyed.
        /// </summary>
        /// <param name="target">The target win.forms control.</param>
        public void SubscribeAllOnControl(System.Windows.Forms.Control target)
        {
            target.EnsureNotNull(nameof(target));

            IEnumerable<MessageSubscription> generatedSubscriptions = null;

            // Create handler delegates
            EventHandler onHandleCreated = (sender, eArgs) =>
            {
                if (generatedSubscriptions == null)
                {
                    generatedSubscriptions = SubscribeAll(target);
                }
            };
            EventHandler onHandleDestroyed = (inner, eArgs) =>
            {
                if (generatedSubscriptions != null)
                {
                    foreach(MessageSubscription actSubscription in generatedSubscriptions)
                    {
                        actSubscription.Unsubscribe();
                    }
                    generatedSubscriptions = null;
                }
            };

            // Attach to events and subscribe on message, if handle is already created
            target.HandleCreated += onHandleCreated;
            target.HandleDestroyed += onHandleDestroyed;
            if (target.IsHandleCreated)
            {
                generatedSubscriptions = SubscribeAll(target);
            }
        }
#endif

        /// <summary>
        /// Subscribes all receiver-methods of the given target object to this Messenger.
        /// </summary>
        /// <param name="targetObject">The target object which is to subscribe..</param>
        public IEnumerable<MessageSubscription> SubscribeAll(object targetObject)
        {
            targetObject.EnsureNotNull(nameof(targetObject));

            Type targetObjectType = targetObject.GetType();

            List<MessageSubscription> generatedSubscriptions = new List<MessageSubscription>(16);
            try
            {
                Type typeofMessage = typeof(SeeingSharpMessage);
#if DESKTOP
                foreach (MethodInfo actMethod in targetObjectType.GetMethods(
                    BindingFlags.NonPublic | BindingFlags.Public | 
                    BindingFlags.Instance | BindingFlags.InvokeMethod))
#else
                foreach(MethodInfo actMethod in targetObjectType.GetTypeInfo().GetDeclaredMethods(
                    SeeingSharpConstants.METHOD_NAME_MESSAGE_RECEIVED))
#endif
                {
                    if (!actMethod.Name.Equals(SeeingSharpConstants.METHOD_NAME_MESSAGE_RECEIVED)) { continue; }

                    ParameterInfo[] parameters = actMethod.GetParameters();
                    if (parameters.Length != 1) { continue; }

                    if (!typeofMessage.GetTypeInfo().IsAssignableFrom(
                        parameters[0].ParameterType.GetTypeInfo())) 
                    {
                        continue; 
                    }

                    Type genericAction = typeof(Action<>);
                    Type delegateType = genericAction.MakeGenericType(parameters[0].ParameterType);
                    generatedSubscriptions.Add(this.Subscribe(
                        actMethod.CreateDelegate(delegateType, targetObject),
                        parameters[0].ParameterType));
                }
            }
            catch(Exception)
            {
                foreach(MessageSubscription actSubscription in generatedSubscriptions)
                {
                    actSubscription.Unsubscribe();
                }
                generatedSubscriptions.Clear();
            }

            return generatedSubscriptions;
        }

        /// <summary>
        /// Subscribes to the given MessageType.
        /// </summary>
        /// <typeparam name="MessageType">Type of the message.</typeparam>
        /// <param name="actionOnMessage">Action to perform on incoming message.</param>
        public MessageSubscription Subscribe<MessageType>(Action<MessageType> actionOnMessage)
            where MessageType : SeeingSharpMessage
        {
            actionOnMessage.EnsureNotNull(nameof(actionOnMessage));

            Type currentType = typeof(MessageType);
            return this.Subscribe(actionOnMessage, currentType);
        }

        /// <summary>
        /// Subscribes the given MessageType and executes the action only when the condition is true.
        /// </summary>
        /// <typeparam name="MessageType">The type of the message type.</typeparam>
        /// <param name="condition">The condition.</param>
        /// <param name="actionOnMessage">The messenger.</param>
        /// <returns></returns>
        public MessageSubscription SubscribeWhen<MessageType>(Func<MessageType, bool> condition, Action<MessageType> actionOnMessage)
            where MessageType : SeeingSharpMessage
        {
            condition.EnsureNotNull(nameof(condition));
            actionOnMessage.EnsureNotNull(nameof(actionOnMessage));

            Action<MessageType> filterAction = (message) =>
            {
                if (condition(message))
                {
                    actionOnMessage(message);
                }
            };

            Type currentType = typeof(MessageType);
            return this.Subscribe(filterAction, currentType);
        }

        /// <summary>
        /// Subscribes to the given message type.
        /// </summary>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="actionOnMessage">Action to perform on incoming message.</param>
        public MessageSubscription Subscribe(
            Delegate actionOnMessage, Type messageType)
        {
            actionOnMessage.EnsureNotNull(nameof(actionOnMessage));
            messageType.EnsureNotNull(nameof(messageType));

            if (!messageType.GetTypeInfo().IsSubclassOf(typeof(SeeingSharpMessage))) { throw new ArgumentException("Given message type does not derive from SeeingSharpMessage!"); }

            MessageSubscription newOne = new MessageSubscription(this, messageType, actionOnMessage);
            lock (m_messageSubscriptionsLock)
            {
                if (m_messageSubscriptions.ContainsKey(messageType))
                {
                    m_messageSubscriptions[messageType].Add(newOne);
                }
                else
                {
                    List<MessageSubscription> newList = new List<MessageSubscription>();
                    newList.Add(newOne);
                    m_messageSubscriptions[messageType] = newList;
                }
            }

            return newOne;
        }

#if DESKTOP
        /// <summary>
        /// Subscribes to the given MessageType during the livetime of the given control.
        /// Events OnHandleCreated and OnHandleDestroyed are used for subscribing / unsubscribing.
        /// </summary>
        /// <typeparam name="MessageType">Type of the message.</typeparam>
        /// <param name="actionOnMessage">Action to perform on incoming message.</param>
        /// <param name="target">The target control.</param>
        public void SubscribeOnControl<MessageType>(System.Windows.Forms.Control target, Action<MessageType> actionOnMessage)
            where MessageType : SeeingSharpMessage
        {
            target.EnsureNotNull(nameof(target));
            actionOnMessage.EnsureNotNull(nameof(actionOnMessage));
            MessageSubscription subscription = null;

            //Create handler delegates
            EventHandler onHandleCreated = (sender, eArgs) =>
            {
                if (subscription == null)
                {
                    subscription = Subscribe(actionOnMessage);
                }
            };
            EventHandler onHandleDestroyed = (inner, eArgs) =>
            {
                if (subscription != null)
                {
                    Unsubscribe(subscription);
                    subscription = null;
                }
            };

            //Attach to events and subscribe on message, if handle is already created
            target.HandleCreated += onHandleCreated;
            target.HandleDestroyed += onHandleDestroyed;
            if (target.IsHandleCreated)
            {
                subscription = Subscribe(actionOnMessage);
            }
        }
#endif

        /// <summary>
        /// Clears the given MessageSubscription.
        /// </summary>
        /// <param name="messageSubscription">The subscription to clear.</param>
        public void Unsubscribe(MessageSubscription messageSubscription)
        {
            messageSubscription.EnsureNotNull(nameof(messageSubscription));

            if (messageSubscription != null && !messageSubscription.IsDisposed)
            {
                Type messageType = messageSubscription.MessageType;

                //Remove subscription from internal list

                lock (m_messageSubscriptionsLock)
                {
                    if (m_messageSubscriptions.ContainsKey(messageType))
                    {
                        List<MessageSubscription> listOfSubscriptions = m_messageSubscriptions[messageType];
                        listOfSubscriptions.Remove(messageSubscription);
                        if (listOfSubscriptions.Count == 0)
                        {
                            m_messageSubscriptions.Remove(messageType);
                        }
                    }
                }

                //Clear given subscription
                messageSubscription.Clear();
            }
        }

        /// <summary>
        /// Counts all message subscriptions for the given message type.
        /// </summary>
        /// <typeparam name="MessageType">The type of the message for which to count all subscriptions.</typeparam>
        public int CountSubscriptionsForMessage<MessageType>()
            where MessageType : SeeingSharpMessage
        {
            lock (m_messageSubscriptionsLock)
            {
                List<MessageSubscription> subscriptions = null;
                if (m_messageSubscriptions.TryGetValue(typeof(MessageType), out subscriptions))
                {
                    return subscriptions.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Sends the given message to all subscribers (asynchonous processing).
        /// There is no possibility here to wait for the answer.
        /// </summary>
        public void BeginPublish<MessageType>()
            where MessageType : SeeingSharpMessage, new()
        {
            BeginPublish(new MessageType());
        }

        /// <summary>
        /// Sends the given message to all subscribers (asynchonous processing).
        /// There is no possibility here to wait for the answer.
        /// </summary>
        /// <typeparam name="MessageType">The type of the essage type.</typeparam>
        /// <param name="message">The message.</param>
        public void BeginPublish<MessageType>(
            MessageType message)
            where MessageType : SeeingSharpMessage
        {
            m_syncContext.PostAlsoIfNull(() => Publish(message));
        }

        /// <summary>
        /// Sends the given message to all subscribers (asynchonous processing).
        /// The returned task waits for all synchronous subscriptions.
        /// </summary>
        /// <typeparam name="MessageType">The type of the message.</typeparam>
        public Task PublishAsync<MessageType>()
            where MessageType : SeeingSharpMessage, new()
        {
            return m_syncContext.PostAlsoIfNullAsync(
                () => Publish<MessageType>(),
                ActionIfSyncContextIsNull.InvokeUsingNewTask);
        }

        /// <summary>
        /// Sends the given message to all subscribers (asynchonous processing).
        /// The returned task waits for all synchronous subscriptions.
        /// </summary>
        /// <typeparam name="MessageType">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public Task PublishAsync<MessageType>(
            MessageType message)
            where MessageType : SeeingSharpMessage
        {
            return m_syncContext.PostAlsoIfNullAsync(
                () => Publish(message),
                ActionIfSyncContextIsNull.InvokeUsingNewTask);
        }

        /// <summary>
        /// Sends the given message to all subscribers (synchonous processing).
        /// </summary>
        public void Publish<MessageType>()
            where MessageType : SeeingSharpMessage, new()
        {
            Publish<MessageType>(new MessageType());
        }

        /// <summary>
        /// Sends the given message to all subscribers (synchonous processing).
        /// </summary>
        /// <typeparam name="MessageType">Type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        public void Publish<MessageType>(
            MessageType message)
            where MessageType : SeeingSharpMessage
        {
            PublishInternal<MessageType>(
                message, true);
        }

        /// <summary>
        /// Sends the given message to all subscribers (synchonous processing).
        /// </summary>
        /// <typeparam name="MessageType">Type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="isInitialCall">Is this one the initial call to publish? (false if we are coming from async routing)</param>
        private void PublishInternal<MessageType>(
            MessageType message, bool isInitialCall)
            where MessageType : SeeingSharpMessage
        {
            message.EnsureNotNull(nameof(message));

            try
            {
                // Check whether publich is possible
                if(m_checkBehavior == SeeingSharpMessageThreadingBehavior.EnsureMainSyncContextOnSyncCalls)
                {
                    if (!CompareSynchronizationContexts())
                    {
                        throw new SeeingSharpException(
                            "Unable to perform a synchronous publish call because current " +
                            "SynchronizationContext is set wrong. This indicates that the call " +
                            "comes from a wrong thread!");
                    }
                }

                // Notify all subscribed targets
                Type currentType = typeof(MessageType);

                // Check for correct message sources
                if (isInitialCall)
                {
                    string[] possibleSources = s_messageSources.GetOrAdd(currentType, (inputType) => message.GetPossibleSourceThreads());
                    if (possibleSources.Length > 0)
                    {
                        string mainThreadName = m_messengerName;
                        if (string.IsNullOrEmpty(mainThreadName) ||
                            (Array.IndexOf(possibleSources, mainThreadName) < 0))
                        {
                            throw new InvalidOperationException(string.Format(
                                "The message of type {0} can only be sent by the threads {1}. This Messenger belongs to the thread {2}, so no publish possible!",
                                currentType.FullName,
                                possibleSources.ToCommaSeparatedString(),
                                !string.IsNullOrEmpty(mainThreadName) ? mainThreadName as string : "(empty)"));
                        }
                    }
                }

                // Perform synchronous message handling
                List<MessageSubscription> subscriptionsToTrigger = new List<MessageSubscription>(20);
                lock (m_messageSubscriptionsLock)
                {
                    if (m_messageSubscriptions.ContainsKey(currentType))
                    {
                        // Need to copy the list to avoid problems, when the list is changed during the loop and cross thread accesses
                        subscriptionsToTrigger = new List<MessageSubscription>(m_messageSubscriptions[currentType]);
                    }
                }

                // Trigger all found subscriptions
                List<Exception> occurredExceptions = null;
                for (int loop = 0; loop < subscriptionsToTrigger.Count; loop++)
                {
                    try
                    {
                        subscriptionsToTrigger[loop].Publish(message);
                    }
                    catch (Exception ex)
                    {
                        if (occurredExceptions == null) { occurredExceptions = new List<Exception>(); }
                        occurredExceptions.Add(ex);
                    }
                }

                // Perform further message routing if enabled
                if (isInitialCall)
                {
                    // Get information about message routing
                    string[] asyncTargets = s_messagesAsyncTargets.GetOrAdd(currentType, (inputType) => message.GetAsyncRoutingTargetThreads());
                    string mainThreadName = m_messengerName;
                    for (int loop = 0; loop < asyncTargets.Length; loop++)
                    {
                        string actAsyncTargetName = asyncTargets[loop];
                        if (mainThreadName == actAsyncTargetName) { continue; }

                        SeeingSharpMessenger actAsyncTargetHandler = null;
                        if (s_messengersByName.TryGetValue(actAsyncTargetName, out actAsyncTargetHandler))
                        {
                            SynchronizationContext actSyncContext = actAsyncTargetHandler.m_syncContext;
                            if (actSyncContext == null) { continue; }

                            SeeingSharpMessenger innerHandlerForAsyncCall = actAsyncTargetHandler;
                            actSyncContext.PostAlsoIfNull(() =>
                            {
                                innerHandlerForAsyncCall.PublishInternal(message, false);
                            });
                        }
                    }
                }

                // Notify all exceptions occurred during publish progress
                if (isInitialCall)
                {
                    if ((occurredExceptions != null) &&
                        (occurredExceptions.Count > 0))
                    {
                        throw new MessagePublishException(typeof(MessageType), occurredExceptions);
                    }
                }
            }
            catch (Exception ex)
            {
                // Check whether we have to throw the exception globally
                var globalExceptionHandler = SeeingSharpMessenger.CustomPublishExceptionHandler;
                bool doRaise = true;
                if (globalExceptionHandler != null)
                {
                    try
                    {
                        doRaise = !globalExceptionHandler(this, ex);
                    }
                    catch 
                    {
                        doRaise = true;
                    }
                }

                // Raise the exception to inform caller about it
                if (doRaise) { throw; }
            }
        }

        /// <summary>
        /// Compares the SynchronizationContext of the current thread and of this messenger instance.
        /// </summary>
        private bool CompareSynchronizationContexts()
        {
#if DESKTOP
            if (SynchronizationContext.Current == m_syncContext) { return true; }

            System.Windows.Threading.DispatcherSynchronizationContext left =
                SynchronizationContext.Current as System.Windows.Threading.DispatcherSynchronizationContext;
            System.Windows.Threading.DispatcherSynchronizationContext right =
                m_syncContext as System.Windows.Threading.DispatcherSynchronizationContext;
            if (left == null) { return false; }
            if (right == null) { return false; }

            var leftDispatcher = CommonTools.ReadPrivateMember<System.Windows.Threading.Dispatcher, System.Windows.Threading.DispatcherSynchronizationContext>(
                left, "_dispatcher");
            var rightDispatcher = CommonTools.ReadPrivateMember<System.Windows.Threading.Dispatcher, System.Windows.Threading.DispatcherSynchronizationContext>(
                right, "_dispatcher");

            return leftDispatcher == rightDispatcher;
#else
            return SynchronizationContext.Current == m_syncContext;
#endif
        }

        /// <summary>
        /// Gets or sets the synchronization context on which to publish all messages.
        /// </summary>
        public SynchronizationContext SyncContext
        {
            get { return m_syncContext; }
        }

        /// <summary>
        /// Gets the current threading behavior of this Messenger.
        /// </summary>
        public SeeingSharpMessageThreadingBehavior ThreadingBehavior
        {
            get { return m_checkBehavior; }
        }

        /// <summary>
        /// Gets the name of the associated thread.
        /// </summary>
        public string MainThreadName
        {
            get
            {
                return m_messengerName;
            }
        }

        /// <summary>
        /// Counts all message subscriptions.
        /// </summary>
        public int CountSubscriptions
        {
            get
            {
                lock (m_messageSubscriptionsLock)
                {
                    int totalCount = 0;
                    foreach (var actKeyValuePair in m_messageSubscriptions)
                    {
                        totalCount += actKeyValuePair.Value.Count;
                    }
                    return totalCount;
                }
            }
        }

        /// <summary>
        /// Gets the total count of globally registered messengers.
        /// </summary>
        public static int CountGlobalMessengers
        {
            get { return s_messengersByName.Count; }
        }
    }
}
