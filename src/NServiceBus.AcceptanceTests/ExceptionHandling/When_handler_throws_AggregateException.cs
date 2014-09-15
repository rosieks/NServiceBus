﻿namespace NServiceBus.AcceptanceTests.ManageFailures
{
    using System;
    using System.Runtime.CompilerServices;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_handler_throws_AggregateException : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exact_AggregateException_exception_from_handler()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                .Done(c => c.ExceptionReceived)
                .Run();
            Assert.AreEqual(typeof(AggregateException), context.ExceptionType);
            Assert.AreEqual(typeof(Exception), context.InnerExceptionType);
            Assert.AreEqual("My Exception", context.ExceptionMessage);
            Assert.AreEqual("My Inner Exception", context.InnerExceptionMessage);
      
#if (!DEBUG)
            StackTraceAssert.AreEqual(
                @"at NServiceBus.AcceptanceTests.ManageFailures.When_handler_throws_AggregateException.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.MessageHandlerRegistry.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.Unicast.Behaviors.InvokeHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.SetCurrentMessageBeingHandledBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.LoadHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.MessageMutator.ApplyIncomingMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Messages.ExecuteLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Messages.DeserializeLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.CallbackInvocationBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.MessageMutator.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.SubscriptionReceiverBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)
at NServiceBus.Transports.Msmq.MsmqDequeueStrategy.Action()", context.StackTrace);

            StackTraceAssert.AreEqual(
                @"at NServiceBus.AcceptanceTests.ManageFailures.When_handler_throws_AggregateException.Endpoint.Handler.MethodThatThrows()
at NServiceBus.AcceptanceTests.ManageFailures.When_handler_throws_AggregateException.Endpoint.Handler.Handle(Message message)", context.InnerStackTrace);
#endif
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public string InnerStackTrace { get; set; }
            public Type InnerExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public string InnerExceptionMessage { get; set; }
            public Type ExceptionType { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c =>
                    {
                        c.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                    });
                    b.DisableFeature<TimeoutManager>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                    Context.ExceptionMessage = e.Message;
                    Context.StackTrace = e.StackTrace;
                    Context.ExceptionType = e.GetType();
                    if (e.InnerException != null)
                    {
                        Context.InnerExceptionMessage = e.InnerException.Message;
                        Context.InnerExceptionType = e.InnerException.GetType();
                        Context.InnerStackTrace = e.InnerException.StackTrace;
                    }
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Handle(Message message)
                {
                    try
                    {
                        MethodThatThrows();
                    }
                    catch (Exception exception)
                    {
                        throw new AggregateException("My Exception", exception);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                void MethodThatThrows()
                {
                    throw new Exception("My Inner Exception");
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
}