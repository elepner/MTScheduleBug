using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MT.Scheduling.Messages;

namespace MT.Scheduling
{
    class Program
    {
        private static readonly string[] Names = new[] {"John", "Dave", "Steve", "Edward", "Ivan", "Dmitry", "Tomas"};
        private static readonly string[] LastNames = new[] { "Doe", "Smith", "O'Neil", "London", "NY", "Six", "Kuznetsov" };
        private static readonly Random Random = new Random();
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(
                    (configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); }))
                .ConfigureServices((hostingContext, serviceCollection) =>
                {
                    serviceCollection.AddMassTransit();
                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString,
                            hostConfiguration => { hostConfiguration.OperationTimeout = TimeSpan.FromSeconds(10); });

                        cfg.SubscriptionEndpoint<UserVerified>(host, "MT.Scheduling.Publisher", configurator =>
                        {
                            configurator.Handler<UserVerified>(context =>
                            {
                                Console.WriteLine($"User verified {context.Message.FirstName} {context.Message.LastName} {context.Message.Timeout}");
                                return Task.CompletedTask;
                            });
                        });

                        EndpointConvention.Map<CreateUserCommand>(new Uri($"{host.Address.ToString()}/testproject/createUserCommand"));
                    }));

                    serviceCollection.AddSingleton<IPublishEndpoint>(provider => provider.GetService<IBusControl>());
                    serviceCollection.AddSingleton<ISendEndpointProvider>(provider => provider.GetService<IBusControl>());
                    
                });
            
            var config = builder.Build();
            var sendEndpointProvider = config.Services.GetService<ISendEndpointProvider>();
            var busControl = config.Services.GetService<IBusControl>();

            await busControl.StartAsync();
            Console.WriteLine("Started bus.");

            
            Console.WriteLine("Press Enter to create user...");
            while (true)
            {
                var name = Names[Random.Next(0, Names.Length)];
                var lastName = LastNames[Random.Next(0, LastNames.Length)];
                var user = new
                {
                    FirstName = name,
                    LastName = lastName
                };
                await sendEndpointProvider.Send<CreateUserCommand>(user);
                Console.WriteLine($"Created user {user.FirstName} {user.LastName}");
                Console.ReadLine();
            }
        }
    }
}
