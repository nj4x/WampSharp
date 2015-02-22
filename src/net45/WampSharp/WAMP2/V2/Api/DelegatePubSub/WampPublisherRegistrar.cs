using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using WampSharp.Core.Listener;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace WampSharp.V2.DelegatePubSub
{
    public class WampPublisherRegistrar
    {
        private readonly IWampRealmProxy mProxy;

        private readonly EventHandlerGenerator mEventHandlerGenerator = new EventHandlerGenerator();

        private readonly ConcurrentDictionary<Tuple<object, string>, PublisherRegistration> mRegistrations =
            new ConcurrentDictionary<Tuple<object, string>, PublisherRegistration>();

        public WampPublisherRegistrar(IWampRealmProxy proxy)
        {
            mProxy = proxy;
            proxy.Monitor.ConnectionBroken += OnConnectionBroken;
            proxy.Monitor.ConnectionError += OnConnectionError;
        }

        private void OnConnectionError(object sender, WampConnectionErrorEventArgs e)
        {
            ClearRegistrations();
        }
        
        private void OnConnectionBroken(object sender, WampSessionCloseEventArgs e)
        {
            ClearRegistrations();
        }

        private void ClearRegistrations()
        {
            foreach (PublisherRegistration registration in mRegistrations.Values)
            {
                registration.Dispose();
            }

            mRegistrations.Clear();
        }

        public void RegisterPublisher(object instance, IPublisherRegistrationInterceptor interceptor)
        {
            AggregateTopics(instance, interceptor, RegisterToEvent);
        }

        public void UnregisterPublisher(object instance, IPublisherRegistrationInterceptor interceptor)
        {
            AggregateTopics(instance, interceptor, UnregisterFromEvent);
        }

        private void AggregateTopics(object instance, IPublisherRegistrationInterceptor interceptor, Action<object, EventInfo, IPublisherRegistrationInterceptor> action)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            Type runtimeType = instance.GetType();

            IEnumerable<Type> typesToExplore = GetTypesToExplore(runtimeType);

            foreach (Type type in typesToExplore)
            {
                foreach (EventInfo @event in type.GetEvents(BindingFlags.Instance |
                                                            BindingFlags.Public))
                {
                    if (interceptor.IsPublisherTopic(@event))
                    {
                        action(instance, @event, interceptor);
                    }
                }
            }
        }

        private IEnumerable<Type> GetTypesToExplore(Type type)
        {
            yield return type;

            foreach (Type @interface in type.GetInterfaces())
            {
                yield return @interface;
            }
        }

        private void RegisterToEvent(object instance, EventInfo @event, IPublisherRegistrationInterceptor interceptor)
        {
            string topic = interceptor.GetTopicUri(@event);

            IWampTopicProxy topicProxy = mProxy.TopicContainer.GetTopicByUri(topic);
            PublishOptions options = interceptor.GetPublishOptions(@event);

            Delegate createdDelegate;

            Type eventHandlerType = @event.EventHandlerType;

            if (IsPositional(eventHandlerType))
            {
                createdDelegate =
                    mEventHandlerGenerator.CreatePositionalDelegate(eventHandlerType, topicProxy, options);
            }
            else
            {
                createdDelegate =
                    mEventHandlerGenerator.CreateKeywordsDelegate(eventHandlerType, topicProxy, options);
            }

            @event.AddEventHandler(instance, createdDelegate);

            PublisherRegistration registration = new PublisherRegistration(instance, createdDelegate, @event, topic);

            mRegistrations.TryAdd(Tuple.Create(instance, topic), registration);
        }

        private bool IsPositional(Type eventHandlerType)
        {
            ICollection<Type> actionTypes =
                new Type[]
                {
                    typeof (Action),
                    typeof (Action<>), 
                    typeof (Action<,>), 
                    typeof (Action<,,>), 
                    typeof (Action<,,,>),
                    typeof (Action<,,,,>), 
                    typeof (Action<,,,,,>), 
                    typeof (Action<,,,,,,>), 
                    typeof (Action<,,,,,,,>),
                    typeof (Action<,,,,,,,,>), 
                    typeof (Action<,,,,,,,,,>), 
                    typeof (Action<,,,,,,,,,,>),
                    typeof (Action<,,,,,,,,,,,>), 
                    typeof (Action<,,,,,,,,,,,,>), 
                    typeof (Action<,,,,,,,,,,,,,>),
                    typeof (Action<,,,,,,,,,,,,,,>), 
                    typeof (Action<,,,,,,,,,,,,,,,>)
                };

            // TODO: add support using the interceptor/an attribute.
            if (!eventHandlerType.IsGenericType)
            {
                return actionTypes.Contains(eventHandlerType);
            }
            else
            {
                return actionTypes.Contains(eventHandlerType.GetGenericTypeDefinition());
            }
        }

        private void UnregisterFromEvent(object instance, EventInfo @event, IPublisherRegistrationInterceptor interceptor)
        {
            string topic = interceptor.GetTopicUri(@event);

            PublisherRegistration registration;

            if (mRegistrations.TryRemove(Tuple.Create(instance, topic), out registration))
            {
                registration.Dispose();
            }
        }

        private class PublisherRegistration : IDisposable
        {
            private readonly object mInstance;
            private readonly Delegate mDelegate;
            private readonly EventInfo mEvent;
            private readonly string mTopicUri;

            public PublisherRegistration(object instance, Delegate @delegate, EventInfo @event, string topicUri)
            {
                mInstance = instance;
                mDelegate = @delegate;
                mEvent = @event;
                mTopicUri = topicUri;
            }

            public void Dispose()
            {
                mEvent.RemoveEventHandler(mInstance, mDelegate);
            }
        }
    }
}