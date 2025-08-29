using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkerService1.Service;
using WorkerService1;



    
        // Load configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<MongoDbService>();

        // Check for test mode
        if (args.Length > 0 && args[0].ToLower() == "test-mongo")
        {
            Console.WriteLine("Running MongoDB test harness...");
            try
            {
                // Initialize MongoDbService
                var mongoDbService = new MongoDbService();

                // Generate seed data
                var properties = PropertySeedData.GenerateSeedData(5);
                logger.LogInformation("Generated {PropertyCount} seed properties for testing.", properties.Count);

                // Test insertion
                await mongoDbService.InsertPropertiesAsync(properties);
                logger.LogInformation("Successfully inserted seed data into MongoDB.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to insert seed data into MongoDB.");
                throw;
            }
            return;
        }

        // Original worker service setup
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<ScrapePageConsumer>();
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
                        try
                        {
                            var ipAddress = System.Net.Dns.GetHostAddresses(host).FirstOrDefault()?.ToString() ?? "failed";
                            Console.WriteLine($"Attempting to connect to RabbitMQ at {host} (IP: {ipAddress})");
                            cfg.Host(host, h =>
                            {
                                h.Username(Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest");
                                h.Password(Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest");
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"RabbitMQ connection failed: {ex.Message}");
                            throw;
                        }

                        cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(10)));
                        cfg.ReceiveEndpoint("scrape-page-queue", e =>
                        {
                            e.ConfigureConsumer<ScrapePageConsumer>(context);
                            e.ConcurrentMessageLimit = 1;
                            e.PrefetchCount = 1;
                        });
                    });
                });
                services.AddScoped<IScraperService, ScraperService>();
                services.AddSingleton<MongoDbService>();
                services.AddSingleton<Worker>();
                services.AddHostedService<Worker>(provider => provider.GetRequiredService<Worker>());
            })
            .Build();

        await host.RunAsync();
 
//IHost host = Host.CreateDefaultBuilder(args)
//    .ConfigureServices(services =>
//    {
//        services.AddMassTransit(x =>
//        {
//            x.AddConsumer<ScrapePageConsumer>();
//            x.UsingRabbitMq((context, cfg) =>
//            {
//                var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
//                try
//                {
//                    var ipAddress = System.Net.Dns.GetHostAddresses(host).FirstOrDefault()?.ToString() ?? "failed";
//                    Console.WriteLine($"Attempting to connect to RabbitMQ at {host} (IP: {ipAddress})");
//                    cfg.Host(host, h =>
//                    {
//                        h.Username(Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest");
//                        h.Password(Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest");
//                    });
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"RabbitMQ connection failed: {ex.Message}");
//                    throw;
//                }

//                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(10)));

//                cfg.ReceiveEndpoint("scrape-page-queue", e =>
//                {
//                    e.ConfigureConsumer<ScrapePageConsumer>(context);
//                    e.ConcurrentMessageLimit = 1; // Process one message at a time
//                    e.PrefetchCount = 1;
//                });
//            });
//        });
//        services.AddScoped<IScraperService, ScraperService>();
//        services.AddSingleton<MongoDbService>();
//        services.AddSingleton<Worker>(); // Register Worker as singleton
//        services.AddHostedService<Worker>(provider => provider.GetRequiredService<Worker>());
//    })
//    .Build();

//await host.RunAsync();