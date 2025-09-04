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

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MongoDB__ConnectionString")
           ?? "mongodb://myuser:mypassword@mongo:27017/scraper?authSource=admin";

            var databaseName = configuration.GetValue<string>("MongoDB__Database")
                ?? "scraper";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName); // <-- now dynamic
            _propertiesCollection = database.GetCollection<Property>("Properties");
        }

        // Method to create index, called after initialization if needed
        public async Task CreateIndexAsync()
        {
            var filter = Builders<Property>.Filter.Empty;
            var count = await _propertiesCollection.CountDocumentsAsync(filter);
            if (count > 0)
            {
                var indexKeys = Builders<Property>.IndexKeys.Ascending(p => p.Meta_Content);
                var indexOptions = new CreateIndexOptions
                {
                    Unique = true,
                    Sparse = true // skip documents where Meta_Content is null
                };
                var indexModel = new CreateIndexModel<Property>(indexKeys, indexOptions);
                await _propertiesCollection.Indexes.CreateOneAsync(indexModel);
            }
        }

        public async Task InsertPropertiesAsync(List<Property> properties)
        {
            var existingIds = await _propertiesCollection
                .Find(Builders<Property>.Filter.In(p => p.MlsNumberNoStealth, properties.Select(x => x.MlsNumberNoStealth)))
                .Project(p => p.MlsNumberNoStealth)
                .ToListAsync();

            var newProperties = properties
                .Where(p => !existingIds.Contains(p.MlsNumberNoStealth))
                .ToList();

            if (newProperties.Count > 0)
            {
                await _propertiesCollection.InsertManyAsync(newProperties);
                Console.WriteLine($"Inserted {newProperties.Count} properties into MongoDB.");
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

        public async Task UpsertPropertiesAsync(List<Property> properties)
        {
            foreach (var property in properties)
            {
                var filter = Builders<Property>.Filter.Eq(p => p.MlsNumberNoStealth, property.MlsNumberNoStealth);
                var options = new ReplaceOptions { IsUpsert = true };
                await _propertiesCollection.ReplaceOneAsync(filter, property, options);
            }
        }
    }
    //public class MongoDbService
    // {
    //     private readonly IMongoCollection<Property> _propertiesCollection;

    //     public MongoDbService()
    //     {
    //         //var client = new MongoClient("mongodb://mongo:27017");
    //         var connectionString =  "mongodb://myuser:mypassword@mongo:27017/scraper?authSource=admin";
    //         var client = new MongoClient(connectionString);
    //         var database = client.GetDatabase("PropertyScraperDb");
    //         _propertiesCollection = database.GetCollection<Property>("Properties");

    //         // Only create index if collection is not empty
    //         if (_propertiesCollection.CountDocuments(FilterDefinition<Property>.Empty) > 0)
    //         {
    //             var indexKeys = Builders<Property>.IndexKeys.Ascending(p => p.Meta_Content);

    //             var indexOptions = new CreateIndexOptions
    //             {
    //                 Unique = true,
    //                 Sparse = true // skip documents where Meta_Content is null
    //             };

    //             var indexModel = new CreateIndexModel<Property>(indexKeys, indexOptions);
    //             _propertiesCollection.Indexes.CreateOne(indexModel);
    //         }
    //     }

    //     public async Task InsertPropertiesAsync(List<Property> properties)
    //     {
    //         var existingIds = await _propertiesCollection
    //            .Find(Builders<Property>.Filter.In(p => p.MlsNumberNoStealth, properties.Select(x => x.MlsNumberNoStealth)))
    //            .Project(p => p.MlsNumberNoStealth)
    //            .ToListAsync();

    //         var newProperties = properties
    //             .Where(p => !existingIds.Contains(p.MlsNumberNoStealth))
    //             .ToList();

    //         if (newProperties.Count > 0)
    //         {
    //             await _propertiesCollection.InsertManyAsync(newProperties);
    //             Console.WriteLine($"Inserted {newProperties.Count} properties into MongoDB.");
    //         }
    //     }

    //     public async Task<long> GetPropertiesCountAsync()
    //     {
    //         return await _propertiesCollection.CountDocumentsAsync(Builders<Property>.Filter.Empty);
    //     }

    //     public async Task<List<Property>> GetAllPropertiesAsync()
    //     {
    //         return await _propertiesCollection.Find(Builders<Property>.Filter.Empty).ToListAsync();
    //     }

    //     public async Task UpsertPropertiesAsync(List<Property> properties)
    //     {
    //         foreach (var property in properties)
    //         {
    //             var filter = Builders<Property>.Filter.Eq(p => p.MlsNumberNoStealth, property.MlsNumberNoStealth);
    //             var options = new ReplaceOptions { IsUpsert = true }; // Insert if not exists
    //             await _propertiesCollection.ReplaceOneAsync(filter, property, options);
    //         }
    //     }
    // }


}
