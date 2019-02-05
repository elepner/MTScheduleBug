using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MT.Scheduling.Messages;

namespace MT.Scheduling.Subscriber
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder().ConfigureHostConfiguration(
                builder => { builder.AddEnvironmentVariables(); }
            ).ConfigureServices((hostingContext, serviceCollection) =>
            {
                string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                {


                    var host = cfg.Host(busConnectionString,
                        hostConfiguration => { hostConfiguration.OperationTimeout = TimeSpan.FromSeconds(10); });

                    cfg.ReceiveEndpoint(host, "testproject/createusercommand", configurator =>
                    {
                        configurator.Handler<CreateUserCommand>(async context =>
                        {
                            Console.WriteLine($"Writing user {context.Message.FirstName} {context.Message.LastName} to some storage...");
                            var executedIn = 10;
                            await context.SchedulePublish<UserVerified>(TimeSpan.FromSeconds(executedIn), new 
                            {
                                context.Message.FirstName,
                                context.Message.LastName,
                                Timeout = executedIn
                            });
                        });
                    });
                    cfg.UseServiceBusMessageScheduler();
                    //cfg.ReceiveEndpoint("ScheduledCommand", configurator =>
                    //{
                    //    configurator.Consumer<ScheduledMessageConsumer>();
                    //});
                }));
            });

            var runtime = hostBuilder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            await runtime.StartAsync();
        }
    }
}
