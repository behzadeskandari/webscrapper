using MassTransit;
using MassTransit.Transports;
using WorkerService1.Service;

namespace WorkerService1
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        public Worker(ILogger<Worker> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting background scraping at {time}", DateTimeOffset.Now);
                    Console.WriteLine("Starting background scraping...");

                    const int maxPages = 1;
                    for (int page = 1; page <= maxPages; page++)
                    {
                        await _publishEndpoint.Publish(new ScrapePageCommand { PageNumber = page }, stoppingToken);
                        Console.WriteLine($"Published ScrapePageCommand for page {page}");
                        // یه تاخیر کوچک برای جلوگیری از فشار زیاد به RabbitMQ
                        await Task.Delay(100, stoppingToken);
                    }

                    _logger.LogInformation("Published all page scrape commands.");
                    Console.WriteLine("All page scrape commands published. Waiting for consumers to process...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background scraping.");
                    Console.WriteLine($"Background scraping failed: {ex.Message}");
                }

                // صبر تا اجرای بعدی (۱۲ ساعت)
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }

        }
    }
}
