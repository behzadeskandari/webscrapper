using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using MongoDB.Driver;
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
                    var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<ScraperService>();
                    })
                    .Build();

                var scraperService = host.Services.GetRequiredService<ScraperService>();
                // Scrape properties (e.g., for page 1)
                int pageToScrape = 1;
                //var properties = await scraperService.ScrapePropertiesAsync(pageToScrape);
                List<WorkerService1.Property> allProperties = new List<WorkerService1.Property>();
                var baseUrls = new List<(string Url, string Type)>
                {
                    ("https://www.centris.ca/en/commercial-properties~for-sale?uc=1", "commercial"),
                    ("https://www.centris.ca/en/properties~for-sale?uc=1", "residential")
                };

                foreach (var (url, type) in baseUrls)
                {
                   List<WorkerService1.Property> prop = await scraperService.ScrapePropertiesAsync(pageToScrape, type, url); // Pass page, type, and URL
                    allProperties.AddRange(prop);
                }
                if (allProperties == null || allProperties.Count == 0)
                {
                    Console.WriteLine("No properties scraped. Check logs for errors.");
                    return;
                }

                // Print scraped properties
                foreach (WorkerService1.Property prop in allProperties)
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
                    Console.WriteLine($"Broker Name: {prop.BrokerNames}");
                    Console.WriteLine($"Broker Phone: {prop.BrokerPhones}");
                    Console.WriteLine($"Photo Count: {prop.PhotoCount}");
                    Console.WriteLine($"Additional Photo URLs: {string.Join(", ", prop.AdditionalPhotoUrls)}");
                    Console.WriteLine(new string('-', 50));
                }
                var mongoDbService = host.Services.GetRequiredService<WorkerService1.MongoDbService>();
                await mongoDbService.InsertPropertiesAsync(allProperties);
                Console.WriteLine($"Scraping completed. Total properties: {allProperties.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Main: {ex.Message}");
            }
        }

      
    }

}