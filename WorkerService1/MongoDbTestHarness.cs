using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public class MongoDbTestHarness
    {
        public static async Task TestMongoDbConnection(IConfiguration configuration, ILogger<MongoDbService> logger)
        {
            try
            {
                // Initialize MongoDbService
                var mongoDbService = new MongoDbService(configuration);

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
        }
    }
}
