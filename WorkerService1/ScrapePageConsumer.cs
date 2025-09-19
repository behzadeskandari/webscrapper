using MassTransit;
using Microsoft.Extensions.Logging;
using WorkerService1.Service;
using Polly;

namespace WorkerService1
{
    public class ScrapePageConsumer : IConsumer<ScrapePageCommand>
    {
        private readonly IScraperService _scraperService;
        private readonly MongoDbService _mongoDbService;
        private readonly ILogger<ScrapePageConsumer> _logger;

        public ScrapePageConsumer(IScraperService scraperService, MongoDbService mongoDbService, ILogger<ScrapePageConsumer> logger)
        {
            _scraperService = scraperService ?? throw new ArgumentNullException(nameof(scraperService));
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task Consume(ConsumeContext<ScrapePageCommand> context)
        {
            var page = context.Message.PageNumber;
            _logger.LogInformation("Processing page {PageNumber}...", page);

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, ctx) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} for page {PageNumber} after {TimeSpan} due to error: {ErrorMessage}",
                            retryCount, page, timeSpan, exception.Message);
                    });

            try
            {
                List<Property> allProperties = new List<Property>();
                var baseUrls = new List<(string Url, string Type)>
                {
                    ("https://www.centris.ca/en/properties~for-sale?uc=1", "residential"),
                    ("https://www.centris.ca/en/commercial-properties~for-sale?uc=1", "commercial")
                };

                foreach (var (url, type) in baseUrls)
                {
                    _logger.LogDebug("Scraping {PropertyType} properties for page {PageNumber} from {Url}...", type, page, url);
                    List<Property> properties = await retryPolicy.ExecuteAsync(async () =>
                    {
                        return await _scraperService.ScrapePropertiesAsync(page, type, url); // Pass page, type, and URL
                    });

                    if (properties != null && properties.Count > 0)
                    {
                        allProperties.AddRange(properties);
                        _logger.LogInformation("Scraped {PropertyCount} {PropertyType} properties from page {PageNumber}.", properties.Count, type, page);
                    }
                    else
                    {
                        _logger.LogInformation("No {PropertyType} properties found on page {PageNumber}.", type, page);
                    }
                }

                if (allProperties.Count > 0)
                {
                    await _mongoDbService.InsertPropertiesAsync(allProperties);
                    _logger.LogInformation("Inserted {PropertyCount} properties (residential and commercial) from page {PageNumber} into MongoDB.", allProperties.Count, page);
                }
                else
                {
                    _logger.LogInformation("No properties found on page {PageNumber} for residential or commercial.", page);
                }
                //List<Property> properties = await retryPolicy.ExecuteAsync(async () =>
                //{
                //    _logger.LogDebug("Scraping properties for page {PageNumber}...", page);
                //    return await _scraperService.ScrapePropertiesAsync(page); // Pass page number
                //});

                //if (properties != null && properties.Count > 0)
                //{
                //    await _mongoDbService.InsertPropertiesAsync(properties);
                //    _logger.LogInformation("Inserted {PropertyCount} properties from page {PageNumber} into MongoDB.", properties.Count, page);
                //}
                //else
                //{
                //    _logger.LogInformation("No properties found on page {PageNumber}.", page);
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process page {PageNumber} after retries.", page);
                throw;
            }
        }

    }
}
