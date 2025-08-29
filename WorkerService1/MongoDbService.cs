using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
 
namespace WorkerService1
{
   public class MongoDbService
    {
        private readonly IMongoCollection<Property> _propertiesCollection;

        public MongoDbService()
        {
            var client = new MongoClient("mongodb://mongo:27017");
            var database = client.GetDatabase("PropertyScraperDb");
            _propertiesCollection = database.GetCollection<Property>("Properties");
        }

        public async Task InsertPropertiesAsync(List<Property> properties)
        {
            if (properties != null && properties.Count > 0)
            {
                await _propertiesCollection.InsertManyAsync(properties);
                Console.WriteLine($"Inserted {properties.Count} properties into MongoDB.");
            }
        }

        public async Task<long> GetPropertiesCountAsync()
        {
            return await _propertiesCollection.CountDocumentsAsync(Builders<Property>.Filter.Empty);
        }

        public async Task<List<Property>> GetAllPropertiesAsync()
        {
            return await _propertiesCollection.Find(Builders<Property>.Filter.Empty).ToListAsync();
        }
    }
}
