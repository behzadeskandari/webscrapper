using MassTransit;
using WorkerService1.Service;
using webscrapper;

namespace WorkerService1
{
    public class ScrapePageConsumer : IConsumer<ScrapePageCommand>
    {
        private readonly IScraperService _scraperService;
        private readonly MongoDbService _mongoDbService;

        public ScrapePageConsumer(IScraperService scraperService, MongoDbService mongoDbService)
        {
            _scraperService = scraperService;
            _mongoDbService = mongoDbService;
        }

        public async Task Consume(ConsumeContext<ScrapePageCommand> context)
        {
            var page = context.Message.PageNumber;
            try
            {
                Console.WriteLine($"Processing page {page}...");
                var properties = await _scraperService.ScrapePropertiesAsync(page);
                if (properties != null && properties.Count > 0)
                {
                    await _mongoDbService.InsertPropertiesAsync(properties);
                    Console.WriteLine($"Inserted {properties.Count} properties from page {page} into MongoDB.");
                }
                else
                {
                    Console.WriteLine($"No properties found on page {page}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing page {page}: {ex.Message}");
            }
        }
    }
}
