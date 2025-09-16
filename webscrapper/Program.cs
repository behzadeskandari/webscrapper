using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading;
using HtmlAgilityPack;
using Microsoft.VisualBasic;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Network;
using OpenQA.Selenium.BiDi.Script;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumUndetectedChromeDriver;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using webscrapper;
using WorkerService1.Service;
using static System.Runtime.InteropServices.JavaScript.JSType;
using UndetectedChromeDriver = SeleniumUndetectedChromeDriver.UndetectedChromeDriver;


namespace Scrapper
{

    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting the scraper...");

                // Instantiate the scraper service
                IScraperService scraperService = new ScraperService();

                // Scrape properties (e.g., for page 1)
                int pageToScrape = 1;
                var properties = await scraperService.ScrapePropertiesAsync(pageToScrape);

                if (properties == null || properties.Count == 0)
                {
                    Console.WriteLine("No properties scraped. Check logs for errors.");
                    return;
                }

                // Print scraped properties
                foreach (var prop in properties)
                {
                    Console.WriteLine("Property Details:");
                    Console.WriteLine($"Meta Content: {prop.Meta_Content}");
                    Console.WriteLine($"Property URL: {prop.Property_URl}");
                    Console.WriteLine($"Property Image: {prop.PropertyImage}");
                    Console.WriteLine($"MLS Number: {prop.MlsNumberNoStealth}");
                    Console.WriteLine($"Price Currency: {prop.PriceCurrency}");
                    Console.WriteLine($"Price: {prop.Price}");
                    Console.WriteLine($"Category: {prop.Category}");
                    Console.WriteLine($"Address: {prop.Address}");
                    Console.WriteLine($"Organization Name: {prop.Orgazination_Name}");
                    Console.WriteLine("Amenities:");
                    foreach (var amenity in prop.Amenities)
                    {
                        Console.WriteLine($"  {amenity.Key}: {amenity.Value}");
                    }
                    Console.WriteLine($"Latitude: {prop.Latetude}");
                    Console.WriteLine($"Longitude: {prop.Longitude}");
                    Console.WriteLine($"Description: {prop.Description}");
                    Console.WriteLine("Financial Details:");
                    foreach (var detail in prop.FinancialDetails)
                    {
                        Console.WriteLine($"  {detail.Key}: {detail.Value}");
                    }
                    Console.WriteLine($"Broker Name: {prop.BrokerName}");
                    Console.WriteLine($"Broker Phone: {prop.BrokerPhone}");
                    Console.WriteLine($"Photo Count: {prop.PhotoCount}");
                    Console.WriteLine($"Additional Photo URLs: {string.Join(", ", prop.AdditionalPhotoUrls)}");
                    Console.WriteLine(new string('-', 50));
                }

                // Save to MongoDB
                await SaveToMongoDB(properties);

                Console.WriteLine($"Scraping completed. Total properties: {properties.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Main: {ex.Message}");
            }
        }

        private static async Task SaveToMongoDB(List<Property> properties)
        {
            try
            {
                // MongoDB connection string (replace with your actual connection string)
                string connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017";
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("PropertyDB"); // Replace with your database name
                var collection = database.GetCollection<Property>("Properties"); // Replace with your collection name

                // Insert properties into MongoDB
                await collection.InsertManyAsync(properties);
                Console.WriteLine($"Successfully saved {properties.Count} properties to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to MongoDB: {ex.Message}");
            }
        }
    }

}