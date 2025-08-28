using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkerService1.Service;
using WorkerService1;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ScrapePageConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq://rabbitmq");
                cfg.ReceiveEndpoint("scrape-page-queue", e =>
                {
                    e.ConfigureConsumer<ScrapePageConsumer>(context);
                });
            });
        });

        services.AddScoped<IScraperService, ScraperService>();
        services.AddSingleton<MongoDbService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();