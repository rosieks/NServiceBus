﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.IO;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_dynamic_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_round_robin()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) =>
                    {
                        bus.Send(new Request());
                    }))
                    .WithEndpoint<Receiver1>()
                    .WithEndpoint<Receiver2>()
                    .Done(c => c.Receiver1TimesCalled > 4 && c.Receiver2TimesCalled > 4)
                    .Run();

            Assert.IsTrue(context.Receiver1TimesCalled > 4);
            Assert.IsTrue(context.Receiver2TimesCalled > 4);
        }

        public class Context : ScenarioContext
        {
            public int Receiver1TimesCalled { get; set; }
            public int Receiver2TimesCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;

                File.WriteAllLines(Path.Combine(basePath, "Receiver.txt"), new[]
                {
                    "DynamicRouting.Receiver1",
                    "DynamicRouting.Receiver2"
                });

                EndpointSetup<DefaultServer>(c => c.UseDynamicRouting<FileBasedDynamicRouting>()
                    .LookForFilesIn(basePath)
                    .WithTranslator(address => "Receiver"))
                    .AddMapping<Request>(typeof(Receiver1));
            }

            public class ResponseHandler : IHandleMessages<Response>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(Response message)
                {
                    switch (message.EndpointName)
                    {
                        case "Receiver1":
                            Context.Receiver1TimesCalled++;
                            break;
                        case "Receiver2":
                            Context.Receiver2TimesCalled++;
                            break;
                    }

                    Bus.Send(new Request());
                }
            }
        }

        public class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.Receiver1"));
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }

                public void Handle(Request message)
                {
                    Bus.Reply(new Response
                    {
                        EndpointName = "Receiver1"
                    });
                }
            }
        }

        public class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.Receiver2"));
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }

                public void Handle(Request message)
                {
                    Bus.Reply(new Response
                    {
                        EndpointName = "Receiver2"
                    });
                }
            }
        }

        [Serializable]
        public class Request : ICommand
        {
        }

        [Serializable]
        public class Response : IMessage
        {
            public string EndpointName { get; set; }
        }
        
    }
}
