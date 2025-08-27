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
using static System.Runtime.InteropServices.JavaScript.JSType;
using UndetectedChromeDriver = SeleniumUndetectedChromeDriver.UndetectedChromeDriver;


namespace Scrapper
{
    //public class Program
    //{

    //    static void Main(string[] args)
    //    {

    //        //string url = "https://www.centris.ca/en/properties~for-sale?uc=1";
    //        //var httpClinet  = new HttpClient();

    //        //var html = httpClinet.GetStringAsync(url).Result;

    //        //var htmlDocument = new HtmlDocument();
    //        //htmlDocument.LoadHtml(html);



    //        ////get the data /amenity and other data 
    //        //var amenity = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
    //        //foreach (var item in amenity)
    //        //{
    //        //    var amen = item.InnerHtml.Trim();
    //        //    Console.WriteLine($"amenity is " + amen);
    //        //}
    //        var lengthofPropertyPage = 250;
    //        var properties = new List<Property>();
    //        for (int i = 0; i < 250; i++)
    //        {
    //            string url = $"https://www.centris.ca/en/properties~for-sale?uc={i}";
    //            var httpClient = new HttpClient();
    //            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

    //            var html = httpClient.GetStringAsync(url).Result;

    //            var htmlDocument = new HtmlDocument();
    //            htmlDocument.LoadHtml(html);
    //            var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
    //            if (propertyNodes != null)
    //            {
    //                foreach (var node in propertyNodes)
    //                {
    //                    var property = new Property();

    //                    var metaContentNode = node.SelectSingleNode(".//meta[@itemprop='name']");
    //                    property.Meta_Content = metaContentNode?.GetAttributeValue("content", "");

    //                    var propertyUrlNode = node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']");
    //                    property.Property_URl = propertyUrlNode?.GetAttributeValue("href", "");
    //                    if (!string.IsNullOrEmpty(property.Property_URl))
    //                    {
    //                        property.Property_URl = "https://www.centris.ca" + property.Property_URl; // Prepend base URL
    //                    }

    //                    var imageNode = node.SelectSingleNode(".//img[@itemprop='image']");
    //                    property.PropertyImage = imageNode?.GetAttributeValue("src", "");

    //                    var mlsNode = node.SelectSingleNode(".//div[@id='MlsNumberNoStealth']//p");
    //                    property.MlsNumberNoStealth = mlsNode?.InnerText.Trim();

    //                    var priceCurrencyNode = node.SelectSingleNode(".//meta[@itemprop='priceCurrency']");
    //                    property.PriceCurrency = priceCurrencyNode?.GetAttributeValue("content", "");

    //                    var priceNode = node.SelectSingleNode(".//meta[@itemprop='price']");
    //                    property.Price = priceNode?.GetAttributeValue("content", "");

    //                    var categoryNode = node.SelectSingleNode(".//div[@itemprop='category']//div");
    //                    property.Category = categoryNode?.InnerText.Trim();

    //                    var addressNodes = node.SelectNodes(".//div[@class='address']//div");
    //                    if (addressNodes != null && addressNodes.Count >= 2)
    //                    {
    //                        property.Address = $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}";
    //                    }

    //                    var orgNameNode = node.SelectSingleNode(".//p[@class='organisation-name']");
    //                    property.Orgazination_Name = orgNameNode?.InnerText.Trim();
    //                    property.Amenity = node.InnerHtml.Trim();

    //                    var locationNode = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']");
    //                    property.Latetude = locationNode?.GetAttributeValue("data-lat", "");
    //                    property.Longitude = locationNode?.GetAttributeValue("data-lng", "");

    //                    properties.Add(property);
    //                }
    //            }

    //            foreach (var prop in properties)
    //            {
    //                Console.WriteLine("Property Details:");
    //                Console.WriteLine($"Meta Content: {prop.Meta_Content}");
    //                Console.WriteLine($"Property URL: {prop.Property_URl}");
    //                Console.WriteLine($"Property Image: {prop.PropertyImage}");
    //                Console.WriteLine($"MLS Number: {prop.MlsNumberNoStealth}");
    //                Console.WriteLine($"Price Currency: {prop.PriceCurrency}");
    //                Console.WriteLine($"Price: {prop.Price}");
    //                Console.WriteLine($"Category: {prop.Category}");
    //                Console.WriteLine($"Address: {prop.Address}");
    //                Console.WriteLine($"Organization Name: {prop.Orgazination_Name}");
    //                Console.WriteLine($"Amenity: {prop.Amenity.Substring(0, Math.Min(prop.Amenity.Length, 100))}..."); // Truncated for brevity
    //                Console.WriteLine($"Latitude: {prop.Latetude}");
    //                Console.WriteLine($"Longitude: {prop.Longitude}");
    //                Console.WriteLine(new string('-', 50));
    //            }



    //        }
    //        Console.WriteLine($"Total properties scraped: {properties.Count}");



    //    }


    //}

    //public class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        var properties = new List<Property>();

    //        // Setup WebDriverManager to resolve ChromeDriver
    //        new DriverManager().SetUpDriver(new ChromeConfig());
    //        var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());

    //        // Configure ChromeOptions for undetected-chromedriver
    //        var chromeOptions = new ChromeOptions();
    //        chromeOptions.AddArguments(
    //            "--headless=new",
    //            "--no-sandbox",
    //            "--disable-gpu",
    //            "--disable-dev-shm-usage",
    //            "--disable-blink-features=AutomationControlled",
    //            "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36",
    //            "--window-size=1920,1080",
    //            "--disable-extensions",
    //            "--disable-web-security",
    //            "--ignore-certificate-errors"
    //        );

    //        // Initialize undetected-chromedriver
    //        using var driver = UndetectedChromeDriver.Create(
    //            options: chromeOptions,
    //            driverExecutablePath: chromeDriverPath,
    //            browserExecutablePath: null
    //        );

    //        string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
    //        driver.Navigate().GoToUrl(baseUrl);
    //        System.Threading.Thread.Sleep(3000); // Wait for initial load

    //        int pageCount = 1;
    //        while (true)
    //        {
    //            Console.WriteLine($"Scraping page {pageCount}");

    //            try
    //            {
    //                var html = driver.PageSource;
    //                var htmlDocument = new HtmlDocument();
    //                htmlDocument.LoadHtml(html);

    //                var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
    //                if (propertyNodes != null)
    //                {
    //                    foreach (var node in propertyNodes)
    //                    {
    //                        var property = new Property();

    //                        var metaContentNode = node.SelectSingleNode(".//meta[@itemprop='name']");
    //                        property.Meta_Content = metaContentNode?.GetAttributeValue("content", "");

    //                        var propertyUrlNode = node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']");
    //                        property.Property_URl = propertyUrlNode?.GetAttributeValue("href", "");
    //                        if (!string.IsNullOrEmpty(property.Property_URl))
    //                        {
    //                            property.Property_URl = "https://www.centris.ca" + property.Property_URl; // Prepend base URL
    //                        }

    //                        var imageNode = node.SelectSingleNode(".//img[@itemprop='image']");
    //                        property.PropertyImage = imageNode?.GetAttributeValue("src", "");

    //                        var mlsNode = node.SelectSingleNode(".//div[@id='MlsNumberNoStealth']//p");
    //                        property.MlsNumberNoStealth = mlsNode?.InnerText.Trim();

    //                        var priceCurrencyNode = node.SelectSingleNode(".//meta[@itemprop='priceCurrency']");
    //                        property.PriceCurrency = priceCurrencyNode?.GetAttributeValue("content", "");

    //                        var priceNode = node.SelectSingleNode(".//meta[@itemprop='price']");
    //                        property.Price = priceNode?.GetAttributeValue("content", "");

    //                        var categoryNode = node.SelectSingleNode(".//div[@itemprop='category']//div");
    //                        property.Category = categoryNode?.InnerText.Trim();

    //                        var addressNodes = node.SelectNodes(".//div[@class='address']//div");
    //                        if (addressNodes != null && addressNodes.Count >= 2)
    //                        {
    //                            property.Address = $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}";
    //                        }

    //                        var orgNameNode = node.SelectSingleNode(".//p[@class='organisation-name']");
    //                        property.Orgazination_Name = orgNameNode?.InnerText.Trim();

    //                        property.Amenity = node.InnerHtml.Trim();

    //                        var locationNode = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']");
    //                        property.Latetude = locationNode?.GetAttributeValue("data-lat", "");
    //                        property.Longitude = locationNode?.GetAttributeValue("data-lng", "");

    //                        properties.Add(property);
    //                    }
    //                }

    //                // Check if next button is available and active
    //                IReadOnlyCollection<IWebElement> nextButtons = driver.FindElements(By.XPath("//ul[@class='pager']//li[contains(@class, 'next') and not(contains(@class, 'inactive'))]/a"));
    //                if (nextButtons.Count == 0)
    //                {
    //                    Console.WriteLine("No more pages to scrape.");
    //                    break;
    //                }

    //                // Click the next button
    //                nextButtons.First().Click();
    //                System.Threading.Thread.Sleep(3000); // Wait for page to load after click

    //                pageCount++;
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"Error scraping page {pageCount}: {ex.Message}");
    //                break;
    //            }
    //        }

    //        // Print the properties
    //        foreach (var prop in properties)
    //        {
    //            Console.WriteLine("Property Details:");
    //            Console.WriteLine($"Meta Content: {prop.Meta_Content}");
    //            Console.WriteLine($"Property URL: {prop.Property_URl}");
    //            Console.WriteLine($"Property Image: {prop.PropertyImage}");
    //            Console.WriteLine($"MLS Number: {prop.MlsNumberNoStealth}");
    //            Console.WriteLine($"Price Currency: {prop.PriceCurrency}");
    //            Console.WriteLine($"Price: {prop.Price}");
    //            Console.WriteLine($"Category: {prop.Category}");
    //            Console.WriteLine($"Address: {prop.Address}");
    //            Console.WriteLine($"Organization Name: {prop.Orgazination_Name}");
    //            Console.WriteLine($"Amenity: {prop.Amenity.Substring(0, Math.Min(prop.Amenity.Length, 100))}..."); // Truncated for brevity
    //            Console.WriteLine($"Latitude: {prop.Latetude}");
    //            Console.WriteLine($"Longitude: {prop.Longitude}");
    //            Console.WriteLine(new string('-', 50));
    //        }

    //        Console.WriteLine($"Total properties scraped: {properties.Count}");
    //    }
    //}




    //public class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        var properties = new List<Property>();
    //        const int maxPages = 250; // Based on "1 / 250 +"
    //        const int maxRetries = 5; // Increased retry attempts
    //        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    //        // Setup WebDriverManager to resolve ChromeDriver
    //        Console.WriteLine("Setting up ChromeDriver...");
    //        new DriverManager().SetUpDriver(new ChromeConfig());
    //        var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());

    //        // Configure ChromeOptions for undetected-chromedriver
    //        var chromeOptions = new ChromeOptions();
    //        chromeOptions.AddArguments(
    //            "--headless=new",
    //            "--no-sandbox",
    //            "--disable-gpu",
    //            "--disable-dev-shm-usage",
    //            "--disable-blink-features=AutomationControlled",
    //            "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36",
    //            "--window-size=1920,1080",
    //            "--disable-extensions",
    //            "--disable-web-security",
    //            "--ignore-certificate-errors"
    //        );

    //        // Initialize undetected-chromedriver
    //        Console.WriteLine("Initializing browser...");
    //        using var driver = UndetectedChromeDriver.Create(
    //            options: chromeOptions,
    //            driverExecutablePath: chromeDriverPath,
    //            browserExecutablePath: null
    //        );

    //        // Create WebDriverWait instance
    //        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20)); // Increased timeout

    //        // Navigate to the initial page
    //        string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
    //        Console.WriteLine($"Navigating to {baseUrl}...");
    //        try
    //        {
    //            driver.Navigate().GoToUrl(baseUrl);
    //            System.Threading.Thread.Sleep(10000); // Increased wait for Cloudflare

    //            // Check for Cloudflare CAPTCHA or access denied
    //            if (driver.PageSource.Contains("cf-captcha") || driver.PageSource.Contains("Access denied"))
    //            {
    //                Console.WriteLine("Cloudflare CAPTCHA or access denied detected. Consider using a scraping API or proxies.");
    //                File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
    //                return;
    //            }

    //            // Handle Didomi pop-up initially
    //            //DismissDidomiPopup(driver, wait, maxRetries, timestamp);

    //            int pageCount = 1;
    //            while (pageCount <= maxPages)
    //            {
    //                Console.WriteLine($"Scraping page {pageCount}...");

    //                try
    //                {
    //                    // Recheck for Didomi pop-up on each page
    //                    //DismissDidomiPopup(driver, wait, maxRetries, timestamp);

    //                    // Wait for at least 20 properties to load
    //                    wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 15);

    //                    // Parse current page
    //                    var html = driver.PageSource;
    //                    var htmlDocument = new HtmlDocument();
    //                    htmlDocument.LoadHtml(html);

    //                    // Scrape properties
    //                    var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
    //                    if (propertyNodes != null)
    //                    {
    //                        foreach (var node in propertyNodes)
    //                        {
    //                            var property = new Property
    //                            {
    //                                Meta_Content = node.SelectSingleNode(".//meta[@itemprop='name']")?.GetAttributeValue("content", ""),
    //                                //Property_URl = node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", "") ?? "",
    //                                Property_URl = string.IsNullOrEmpty(node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", ""))
    //                                    ? ""
    //                                    : "https://www.centris.ca" + node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", ""),
    //                                PropertyImage = node.SelectSingleNode(".//img[@itemprop='image']")?.GetAttributeValue("src", ""),
    //                                MlsNumberNoStealth = node.SelectSingleNode(".//div[@id='MlsNumberNoStealth']//p")?.InnerText.Trim(),
    //                                PriceCurrency = node.SelectSingleNode(".//meta[@itemprop='priceCurrency']")?.GetAttributeValue("content", ""),
    //                                Price = node.SelectSingleNode(".//meta[@itemprop='price']")?.GetAttributeValue("content", ""),
    //                                Category = node.SelectSingleNode(".//div[@itemprop='category']//div")?.InnerText.Trim(),
    //                                Address = node.SelectNodes(".//div[@class='address']//div") is HtmlNodeCollection addressNodes && addressNodes.Count >= 2
    //                                    ? $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}"
    //                                    : "",
    //                                Orgazination_Name = node.SelectSingleNode(".//p[@class='organisation-name']")?.InnerText.Trim(),
    //                                Amenity = node.InnerHtml.Trim(),
    //                                Latetude = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lat", ""),
    //                                Longitude = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lng", "")
    //                            };

    //                            properties.Add(property);
    //                        }
    //                        Console.WriteLine($"Scraped {propertyNodes.Count} properties on page {pageCount}.");
    //                    }
    //                    else
    //                    {
    //                        Console.WriteLine($"No properties found on page {pageCount}.");
    //                        File.WriteAllText($"page_{pageCount}_no_properties_{timestamp}.html", driver.PageSource);
    //                    }

    //                    // Check for next button
    //                    IWebElement nextButton;
    //                    try
    //                    {
    //                        nextButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//ul[@class='pager']//li[contains(@class, 'next') and not(contains(@class, 'inactive'))]/a")));
    //                    }
    //                    catch (WebDriverTimeoutException)
    //                    {
    //                        Console.WriteLine("No more pages to scrape (next button is inactive or not found).");
    //                        File.WriteAllText($"page_{pageCount}_no_next_button_{timestamp}.html", driver.PageSource);
    //                        break;
    //                    }

    //                    // Click the next button with retry logic
    //                    bool clicked = false;
    //                    for (int attempt = 1; attempt <= maxRetries; attempt++)
    //                    {
    //                        Console.WriteLine($"Clicking next button (attempt {attempt}/{maxRetries})...");
    //                        try
    //                        {
    //                            // Scroll to ensure button is in view
    //                            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", nextButton);
    //                            System.Threading.Thread.Sleep(1500);

    //                            // Try regular click
    //                            nextButton.Click();
    //                            clicked = true;
    //                            System.Threading.Thread.Sleep(7000); // Increased wait for page load
    //                            break;
    //                        }
    //                        catch (ElementClickInterceptedException ex)
    //                        {
    //                            Console.WriteLine($"Next button click intercepted: {ex.Message}");
    //                            // Recheck for Didomi pop-up
    //                            DismissDidomiPopup(driver, wait, maxRetries, timestamp);
    //                            if (attempt == maxRetries)
    //                            {
    //                                Console.WriteLine("Max retries reached for clicking next button.");
    //                                File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
    //                                break;
    //                            }
    //                            // Try JavaScript click
    //                            try
    //                            {
    //                                driver.ExecuteScript("arguments[0].click();", nextButton);
    //                                clicked = true;
    //                                System.Threading.Thread.Sleep(7000);
    //                                break;
    //                            }
    //                            catch (Exception jsEx)
    //                            {
    //                                Console.WriteLine($"JavaScript click failed: {jsEx.Message}");
    //                            }
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                            Console.WriteLine($"Failed to click next button: {ex.Message}");
    //                            File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
    //                            break;
    //                        }
    //                    }

    //                    if (!clicked)
    //                    {
    //                        Console.WriteLine("Failed to click next button after retries.");
    //                        break;
    //                    }

    //                    pageCount++;
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine($"Error scraping page {pageCount}: {ex.Message}");
    //                    File.WriteAllText($"page_{pageCount}_error_{timestamp}.html", driver.PageSource);
    //                    break;
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Selenium setup or initial navigation failed: {ex.Message}");
    //            File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
    //            return;
    //        }
    //        finally
    //        {
    //            driver?.Quit();
    //        }

    //        // Print the properties
    //        foreach (var prop in properties)
    //        {
    //            Console.WriteLine("Property Details:");
    //            Console.WriteLine($"Meta Content: {prop.Meta_Content}");
    //            Console.WriteLine($"Property URL: {prop.Property_URl}");
    //            Console.WriteLine($"Property Image: {prop.PropertyImage}");
    //            Console.WriteLine($"MLS Number: {prop.MlsNumberNoStealth}");
    //            Console.WriteLine($"Price Currency: {prop.PriceCurrency}");
    //            Console.WriteLine($"Price: {prop.Price}");
    //            Console.WriteLine($"Category: {prop.Category}");
    //            Console.WriteLine($"Address: {prop.Address}");
    //            Console.WriteLine($"Organization Name: {prop.Orgazination_Name}");
    //            Console.WriteLine($"Amenity: {prop.Amenity.Substring(0, Math.Min(prop.Amenity.Length, 100))}...");
    //            Console.WriteLine($"Latitude: {prop.Latetude}");
    //            Console.WriteLine($"Longitude: {prop.Longitude}");
    //            Console.WriteLine(new string('-', 50));
    //        }

    //        Console.WriteLine($"Total properties scraped: {properties.Count}");

    //        // Save to JSON
    //        File.WriteAllText($"properties_{timestamp}.json", JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true }));
    //    }

    //    private static void DismissDidomiPopup(IWebDriver driver, WebDriverWait wait, int maxRetries, string timestamp)
    //    {
    //        for (int attempt = 1; attempt <= maxRetries; attempt++)
    //        {
    //            try
    //            {
    //                // Try clicking a consent button
    //                var consentButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(
    //                    "//button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'accept') " +
    //                    "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'agree') " +
    //                    "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ok') " +
    //                    "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'save') " +
    //                    "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'continue') " +
    //                    "or contains(@class, 'didomi-button') or contains(@class, 'didomi-consent') or contains(@id, 'didomi-notice-agree-button') " +
    //                    "or contains(@id, 'didomi-consent-popup-continue') or contains(@id, 'didomi-consent-popup-accept')] " +
    //                    "| //div[contains(@class, 'didomi-popup') or @id='didomi-host' or @id='didomi-popup']//button"
    //                )));
    //                if (consentButton != null)
    //                {
    //                    Console.WriteLine($"Dismissing Didomi pop-up (attempt {attempt}/{maxRetries})...");
    //                    try
    //                    {
    //                        driver.ExecuteJavaScript("arguments[0].scrollIntoView({block: 'center'});", consentButton);
    //                        System.Threading.Thread.Sleep(1500);
    //                        consentButton.Click();
    //                        System.Threading.Thread.Sleep(2000); // Increased wait for pop-up to close
    //                                                             // Verify pop-up is gone
    //                        if (driver.FindElements(By.XPath("//div[contains(@class, 'didomi-popup') or @id='didomi-host' or @class='didomi-consent-popup-header']")).Count == 0)
    //                        {
    //                            Console.WriteLine("Didomi pop-up successfully dismissed.");
    //                            return;
    //                        }
    //                    }
    //                    catch (ElementClickInterceptedException)
    //                    {
    //                        Console.WriteLine("Consent button click intercepted, trying JavaScript click...");
    //                        driver.ExecuteJavaScript("arguments[0].click();", consentButton);
    //                        System.Threading.Thread.Sleep(2000);
    //                        if (driver.FindElements(By.XPath("//div[contains(@class, 'didomi-popup') or @id='didomi-host' or @class='didomi-consent-popup-header']")).Count == 0)
    //                        {
    //                            Console.WriteLine("Didomi pop-up successfully dismissed via JavaScript.");
    //                            return;
    //                        }
    //                    }
    //                }
    //            }
    //            catch (WebDriverTimeoutException)
    //            {
    //                Console.WriteLine($"No Didomi pop-up found or already dismissed on attempt {attempt}.");
    //                // Try JavaScript to accept all consents
    //                try
    //                {
    //                    driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
    //                    System.Threading.Thread.Sleep(2000);
    //                    if (driver.FindElements(By.XPath("//div[contains(@class, 'didomi-popup') or @id='didomi-host' or @class='didomi-consent-popup-header']")).Count == 0)
    //                    {
    //                        Console.WriteLine("Didomi pop-up dismissed via Didomi.setUserAgreeToAll().");
    //                        return;
    //                    }
    //                }
    //                catch (Exception jsEx)
    //                {
    //                    Console.WriteLine($"JavaScript consent failed: {jsEx.Message}");
    //                }
    //                break;
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"Error handling Didomi pop-up on attempt {attempt}: {ex.Message}");
    //                File.WriteAllText($"didomi_error_attempt_{attempt}_{timestamp}.html", driver.PageSource);
    //            }
    //        }
    //        Console.WriteLine("Max retries reached for Didomi pop-up dismissal.");
    //        // Final attempt to disable Didomi pop-up via JavaScript
    //        try
    //        {
    //            driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
    //            System.Threading.Thread.Sleep(2000);
    //            if (driver.FindElements(By.XPath("//div[contains(@class, 'didomi-popup') or @id='didomi-host' or @class='didomi-consent-popup-header']")).Count == 0)
    //            {
    //                Console.WriteLine("Didomi pop-up dismissed via final Didomi.setUserAgreeToAll().");
    //            }
    //            else
    //            {
    //                Console.WriteLine("Failed to dismiss Didomi pop-up after all attempts.");
    //                File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Final JavaScript consent failed: {ex.Message}");
    //            File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
    //        }
    //    }
    //}

    public class Program
    {
        static void Main(string[] args)
        {
            var properties = new List<Property>();
            const int maxPages = 250; // Based on "1 / 250 +"
            const int maxRetries = 5; // Retry attempts
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Setup WebDriverManager to resolve ChromeDriver
            Console.WriteLine("Setting up ChromeDriver...");
            new DriverManager().SetUpDriver(new ChromeConfig());
            var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());

            // Configure ChromeOptions for undetected-chromedriver
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments(
                "--headless=new",
                "--no-sandbox",
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-blink-features=AutomationControlled",
                "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36",
                "--window-size=1920,1080",
                "--disable-extensions",
                "--disable-web-security",
                "--ignore-certificate-errors"
            );

            // Initialize undetected-chromedriver
            Console.WriteLine("Initializing browser...");
            using var driver = UndetectedChromeDriver.Create(
                options: chromeOptions,
                driverExecutablePath: chromeDriverPath,
                browserExecutablePath: null
            );

            // Create WebDriverWait instance
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Navigate to the initial page
            string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
            Console.WriteLine($"Navigating to {baseUrl}...");
            try
            {
                driver.Navigate().GoToUrl(baseUrl);
                System.Threading.Thread.Sleep(10000); // Wait for Cloudflare

                // Check for Cloudflare CAPTCHA or access denied
                if (driver.PageSource.Contains("cf-captcha") || driver.PageSource.Contains("Access denied"))
                {
                    Console.WriteLine("Cloudflare CAPTCHA or access denied detected. Consider using a scraping API or proxies.");
                    File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
                    return;
                }

                // Handle Didomi pop-up initially
                DismissDidomiPopup(driver, wait, maxRetries, timestamp);

                int pageCount = 1;
                while (pageCount <= maxPages)
                {
                    Console.WriteLine($"Scraping page {pageCount}...");

                    try
                    {
                        // Recheck for Didomi pop-up on each page
                        DismissDidomiPopup(driver, wait, maxRetries, timestamp);

                        // Wait for at least 20 properties to load
                        wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 15);

                        // Parse current page
                        var html = driver.PageSource;
                        var htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(html);

                        // Scrape properties
                        var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
                        if (propertyNodes != null)
                        {
                            foreach (var node in propertyNodes)
                            {
                                var property = new Property
                                {
                                    Meta_Content = node.SelectSingleNode(".//meta[@itemprop='name']")?.GetAttributeValue("content", ""),
                                    Property_URl = string.IsNullOrEmpty(node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", ""))
                                        ? ""
                                        : "https://www.centris.ca" + node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", ""),
                                    PropertyImage = node.SelectSingleNode(".//img[@itemprop='image']")?.GetAttributeValue("src", ""),
                                    MlsNumberNoStealth = node.SelectSingleNode(".//div[@id='MlsNumberNoStealth']//p")?.InnerText.Trim(),
                                    PriceCurrency = node.SelectSingleNode(".//meta[@itemprop='priceCurrency']")?.GetAttributeValue("content", ""),
                                    Price = node.SelectSingleNode(".//meta[@itemprop='price']")?.GetAttributeValue("content", ""),
                                    Category = node.SelectSingleNode(".//div[@itemprop='category']//div")?.InnerText.Trim(),
                                    Address = node.SelectNodes(".//div[@class='address']//div") is HtmlNodeCollection addressNodes && addressNodes.Count >= 2
                                        ? $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}"
                                        : "",
                                    Orgazination_Name = node.SelectSingleNode(".//p[@class='organisation-name']")?.InnerText.Trim(),
                                    Amenity = node.InnerHtml.Trim(),
                                    Latetude = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lat", ""),
                                    Longitude = node.SelectSingleNode(".//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lng", "")
                                };

                                properties.Add(property);
                            }
                            Console.WriteLine($"Scraped {propertyNodes.Count} properties on page {pageCount}.");
                        }
                        else
                        {
                            Console.WriteLine($"No properties found on page {pageCount}.");
                            File.WriteAllText($"page_{pageCount}_no_properties_{timestamp}.html", driver.PageSource);
                        }

                        // Check for next button
                        IWebElement nextButton;
                        try
                        {
                            nextButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//ul[@class='pager']//li[contains(@class, 'next') and not(contains(@class, 'inactive'))]/a")));
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.WriteLine("No more pages to scrape (next button is inactive or not found).");
                            File.WriteAllText($"page_{pageCount}_no_next_button_{timestamp}.html", driver.PageSource);
                            break;
                        }

                        // Click the next button with retry logic
                        bool clicked = false;
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                        {
                            Console.WriteLine($"Clicking next button (attempt {attempt}/{maxRetries})...");
                            try
                            {
                                // Scroll to ensure button is in view
                                driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", nextButton);
                                System.Threading.Thread.Sleep(2000);

                                // Try regular click
                                nextButton.Click();
                                clicked = true;
                                System.Threading.Thread.Sleep(10000); // Increased wait for page load
                                break;
                            }
                            catch (ElementClickInterceptedException ex)
                            {
                                Console.WriteLine($"Next button click intercepted: {ex.Message}");
                                // Recheck for Didomi pop-up
                                DismissDidomiPopup(driver, wait, maxRetries, timestamp);
                                if (attempt == maxRetries)
                                {
                                    Console.WriteLine("Max retries reached for clicking next button.");
                                    File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
                                    break;
                                }
                                // Try JavaScript click
                                try
                                {
                                    driver.ExecuteScript("arguments[0].click();", nextButton);
                                    clicked = true;
                                    System.Threading.Thread.Sleep(10000);
                                    break;
                                }
                                catch (Exception jsEx)
                                {
                                    Console.WriteLine($"JavaScript click failed: {jsEx.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to click next button: {ex.Message}");
                                File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
                                break;
                            }
                        }

                        if (!clicked)
                        {
                            Console.WriteLine("Failed to click next button after retries.");
                            break;
                        }

                        pageCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scraping page {pageCount}: {ex.Message}");
                        File.WriteAllText($"page_{pageCount}_error_{timestamp}.html", driver.PageSource);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Selenium setup or initial navigation failed: {ex.Message}");
                File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
                return;
            }
            finally
            {
                driver?.Quit();
            }

            // Print the properties
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
                Console.WriteLine($"Amenity: {prop.Amenity.Substring(0, Math.Min(prop.Amenity.Length, 100))}...");
                Console.WriteLine($"Latitude: {prop.Latetude}");
                Console.WriteLine($"Longitude: {prop.Longitude}");
                Console.WriteLine(new string('-', 50));
            }

            Console.WriteLine($"Total properties scraped: {properties.Count}");

            // Save to JSON
            File.WriteAllText($"properties_{timestamp}.json", JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void DismissDidomiPopup(IWebDriver driver, WebDriverWait wait, int maxRetries, string timestamp)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Try clicking a consent button
                    var consentButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(
                        "//button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'accept') " +
                        "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'agree') " +
                        "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ok') " +
                        "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'save') " +
                        "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'continue') " +
                        "or contains(@class, 'didomi-button') or contains(@class, 'didomi-consent') or contains(@id, 'didomi-notice-agree-button') " +
                        "or contains(@id, 'didomi-consent-popup-continue') or contains(@id, 'didomi-consent-popup-accept') or @aria-label='Accept'] " +
                        "| //div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host']//button"
                    )));
                    if (consentButton != null)
                    {
                        Console.WriteLine($"Dismissing Didomi pop-up (attempt {attempt}/{maxRetries})...");
                        try
                        {
                            driver.ExecuteJavaScript("arguments[0].scrollIntoView({block: 'center'});", consentButton);
                            System.Threading.Thread.Sleep(2000);
                            consentButton.Click();
                            System.Threading.Thread.Sleep(3000); // Increased wait for pop-up to close
                            // Verify pop-up is gone
                            if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
                            {
                                Console.WriteLine("Didomi pop-up successfully dismissed.");
                                return;
                            }
                        }
                        catch (ElementClickInterceptedException)
                        {
                            Console.WriteLine("Consent button click intercepted, trying JavaScript click...");
                            driver.ExecuteJavaScript("arguments[0].click();", consentButton);
                            System.Threading.Thread.Sleep(3000);
                            if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
                            {
                                Console.WriteLine("Didomi pop-up successfully dismissed via JavaScript.");
                                return;
                            }
                        }
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine($"No Didomi pop-up found or already dismissed on attempt {attempt}.");
                    // Try JavaScript to accept all consents
                    try
                    {
                        driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
                        System.Threading.Thread.Sleep(3000);
                        if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
                        {
                            Console.WriteLine("Didomi pop-up dismissed via Didomi.setUserAgreeToAll().");
                            return;
                        }
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"JavaScript consent failed: {jsEx.Message}");
                    }
                    // Try hiding the pop-up
                    try
                    {
                        driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
                        System.Threading.Thread.Sleep(2000);
                        if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
                        {
                            Console.WriteLine("Didomi pop-up hidden via CSS.");
                            return;
                        }
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"CSS hide failed: {jsEx.Message}");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling Didomi pop-up on attempt {attempt}: {ex.Message}");
                    File.WriteAllText($"didomi_error_attempt_{attempt}_{timestamp}.html", driver.PageSource);
                }
            }
            Console.WriteLine("Max retries reached for Didomi pop-up dismissal.");
            // Final attempt to disable Didomi pop-up via JavaScript
            try
            {
                driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
                driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
                System.Threading.Thread.Sleep(3000);
                if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
                {
                    Console.WriteLine("Didomi pop-up dismissed via final JavaScript.");
                }
                else
                {
                    Console.WriteLine("Failed to dismiss Didomi pop-up after all attempts.");
                    File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Final JavaScript consent failed: {ex.Message}");
                File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
            }
        }
    }

}