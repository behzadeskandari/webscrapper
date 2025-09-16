﻿using System.Text.Json;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumUndetectedChromeDriver;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace WorkerService1.Service
{
    public interface IScraperService
    {
        Task<List<Property>> ScrapePropertiesAsync(int maxPages);

    }


    public class ScraperService : IScraperService
    {

        public async Task<List<Property>> ScrapePropertiesAsync(int page)
        {
            var properties = new List<Property>();
            const int maxRetries = 3;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Terminate any existing ChromeDriver processes
            try
            {
                Console.WriteLine("Cleaning up existing ChromeDriver processes...");
                var processes = System.Diagnostics.Process.GetProcessesByName("chromedriver");
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up ChromeDriver processes: {ex.Message}");
            }

            Console.WriteLine("Setting up ChromeDriver...");
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

            IWebDriver driver = null;
            try
            {
                var chromeDriverPath = Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? "chromedriver.exe"
                    : "/usr/local/bin/chromedriver";
                driver = UndetectedChromeDriver.Create(
                    options: chromeOptions,
                    driverExecutablePath: chromeDriverPath,
                    browserExecutablePath: null
                );

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // Reduced timeout
                string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
                Console.WriteLine($"Navigating to {baseUrl} for page {page}...");

                driver.Navigate().GoToUrl(baseUrl);
                await Task.Delay(5000); // Reduced delay for Cloudflare

                if (driver.PageSource.Contains("cf-captcha") || driver.PageSource.Contains("Access denied"))
                {
                    Console.WriteLine("Cloudflare CAPTCHA or access denied detected.");
                    File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
                    return properties;
                }

                await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

                // Navigate to specific page if needed
                if (page > 1)
                {
                    for (int i = 1; i < page; i++)
                    {
                        try
                        {
                            var nextButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//ul[@class='pager']//li[contains(@class, 'next') and not(contains(@class, 'inactive'))]/a")));
                            driver.ExecuteJavaScript("arguments[0].scrollIntoView({block: 'center'});", nextButton);
                            await Task.Delay(1000);
                            nextButton.Click();
                            await Task.Delay(5000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to navigate to page {page}: {ex.Message}");
                            break;
                        }
                    }
                }

                // Scrape current page
                wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 1);
                var html = driver.PageSource;
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
                if (propertyNodes != null)
                {
                    var propertyUrls = propertyNodes
                        .Select(node => node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']")?.GetAttributeValue("href", ""))
                        .Where(url => !string.IsNullOrEmpty(url))
                        .Select(url => "https://www.centris.ca" + url)
                        .ToList();

                    for (int i = 0; i < propertyUrls.Count; i++)
                    {
                        var propertyUrl = propertyUrls[i];
                        Console.WriteLine($"Scraping property {i + 1}/{propertyUrls.Count}: {propertyUrl}");
                        try
                        {
                            driver.Navigate().GoToUrl(propertyUrl);
                            await Task.Delay(3000); // Reduced delay
                            await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

                            wait.Until(d => d.FindElements(By.XPath("//div[@class='col-lg-12 description']")).Count > 0);
                            var detailHtml = driver.PageSource;
                            var detailDoc = new HtmlDocument();
                            detailDoc.LoadHtml(detailHtml);

                            var property = new Property
                            {
                                Meta_Content = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='name']")?.GetAttributeValue("content", ""),
                                Property_URl = propertyUrl,
                                PropertyImage = detailDoc.DocumentNode.SelectSingleNode("//img[@itemprop='image']")?.GetAttributeValue("src", ""),
                                MlsNumberNoStealth = detailDoc.DocumentNode.SelectSingleNode("//div[@id='MlsNumberNoStealth']//p")?.InnerText.Trim(),
                                PriceCurrency = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='priceCurrency']")?.GetAttributeValue("content", ""),
                                Price = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='price']")?.GetAttributeValue("content", ""),
                                Category = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='category']//div")?.InnerText.Trim(),
                                Address = detailDoc.DocumentNode.SelectNodes("//div[@class='address']//div") is HtmlNodeCollection addressNodes && addressNodes.Count >= 2
        ? $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}"
        : "",
                                Orgazination_Name = detailDoc.DocumentNode.SelectSingleNode("//p[@class='organisation-name']")?.InnerText.Trim(),
                                Amenities = new Dictionary<string, string>(),
                                Latetude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lat", ""),
                                Longitude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lng", ""),
                                // فیلدهای جدید
                                Description = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='description']")?.InnerText.Trim(),
                                FinancialDetails = new Dictionary<string, string>(),
                                BrokerName = detailDoc.DocumentNode.SelectSingleNode("//h1[@class='broker-info__broker-title h5 mb-0']")?.InnerText.Trim(),
                                BrokerPhone = detailDoc.DocumentNode.SelectSingleNode("//a[@itemprop='telephone']")?.GetAttributeValue("content", ""),
                                PhotoCount = int.TryParse(detailDoc.DocumentNode.SelectSingleNode("//button[contains(@class, 'photo-btn') and contains(., 'fa-camera')]")?.InnerText.Replace(" ", "").Replace("\n", "").Split(new[] { "<i" }, StringSplitOptions.None)[0], out var count) ? count : 0,
                                AdditionalPhotoUrls = new List<string>()
                            };

                            // Extract amenities (simplified)
                            var descriptionNode = detailDoc.DocumentNode.SelectSingleNode("//div[@class='col-lg-12 description']");
                            if (descriptionNode != null)
                            {
                                var teaserNodes = descriptionNode.SelectNodes(".//div[@class='row teaser']//div[contains(@class, 'col-lg-3 col-sm-6')]");
                                if (teaserNodes != null)
                                {
                                    foreach (var node in teaserNodes)
                                    {
                                        var className = node.GetAttributeValue("class", "");
                                        var value = node.InnerText.Trim();
                                        if (className.Contains("piece")) property.Amenities["Rooms"] = value;
                                        else if (className.Contains("cac")) property.Amenities["Bedrooms"] = value;
                                        else if (className.Contains("sdb")) property.Amenities["Bathrooms"] = value;
                                        else if (className.Contains("lifestyle"))
                                            property.Amenities["LifestyleScore"] = node.SelectSingleNode(".//span[@class='ll-score-color-default']")?.InnerText.Trim() ?? "?";
                                    }
                                }

                                var caracNodes = descriptionNode.SelectNodes(".//div[@class='row']//div[contains(@class, 'carac-container')]");
                                if (caracNodes != null)
                                {
                                    foreach (var node in caracNodes)
                                    {
                                        var title = node.SelectSingleNode(".//div[@class='carac-title']")?.InnerText.Trim();
                                        var value = node.SelectSingleNode(".//div[@class='carac-value']//span")?.InnerText.Trim();
                                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(value))
                                            property.Amenities[title] = value;
                                    }
                                }

                                var walkscoreNode = descriptionNode.SelectSingleNode(".//div[@class='walkscore']//span");
                                if (walkscoreNode != null)
                                    property.Amenities["WalkScore"] = walkscoreNode.InnerText.Trim();
                            }

                            properties.Add(property);
                            Console.WriteLine($"Scraped property {i + 1}/{propertyUrls.Count}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error scraping {propertyUrl}: {ex.Message}");
                            File.WriteAllText($"detail_error_{i}_{timestamp}.html", driver.PageSource);
                        }

                        // Navigate back to listing page
                        driver.Navigate().GoToUrl(baseUrl);
                        await Task.Delay(3000);
                        await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);
                    }
                }
                else
                {
                    Console.WriteLine($"No properties found on page {page}.");
                    File.WriteAllText($"page_{page}_no_properties_{timestamp}.html", driver.PageSource);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ScrapePropertiesAsync: {ex.Message}");
                File.WriteAllText($"initial_error_{timestamp}.html", driver?.PageSource);
            }
            finally
            {
                driver?.Quit(); // Ensure driver is disposed
            }

            Console.WriteLine($"Total properties scraped: {properties.Count}");
            return properties;
        }
        //public async Task<List<Property>> ScrapePropertiesAsync(int maxPages)
        //{
        //    var properties = new List<Property>();
        //    const int maxRetries = 5; // Retry attempts
        //    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        //    // Setup WebDriverManager to resolve ChromeDriver
        //    Console.WriteLine("Setting up ChromeDriver...");
        //    //new DriverManager().SetUpDriver(new ChromeConfig());
        //    //var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());
        //    var chromeDriverPath = Environment.OSVersion.Platform == PlatformID.Win32NT
        //        ? "chromedriver.exe"
        //        : "/usr/local/bin/chromedriver";
        //    // Configure ChromeOptions for undetected-chromedriver
        //    var chromeOptions = new ChromeOptions();
        //    chromeOptions.AddArguments(
        //        "--headless=new",
        //        "--no-sandbox",
        //        "--disable-gpu",
        //        "--disable-dev-shm-usage",
        //        "--disable-blink-features=AutomationControlled",
        //        "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36",
        //        "--window-size=1920,1080",
        //        "--disable-extensions",
        //        "--disable-web-security",
        //        "--ignore-certificate-errors"
        //    );

        //    // Initialize undetected-chromedriver
        //    Console.WriteLine("Initializing browser...");
        //    using var driver = UndetectedChromeDriver.Create(
        //        options: chromeOptions,
        //        driverExecutablePath: chromeDriverPath,
        //        browserExecutablePath: null
        //    );

        //    // Create WebDriverWait instance
        //    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        //    // Navigate to the initial page
        //    string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
        //    Console.WriteLine($"Navigating to {baseUrl}...");
        //    try
        //    {
        //        driver.Navigate().GoToUrl(baseUrl);
        //        System.Threading.Thread.Sleep(10000); // Wait for Cloudflare

        //        // Check for Cloudflare CAPTCHA or access denied
        //        if (driver.PageSource.Contains("cf-captcha") || driver.PageSource.Contains("Access denied"))
        //        {
        //            Console.WriteLine("Cloudflare CAPTCHA or access denied detected. Consider using a scraping API or proxies.");
        //            File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
        //            return null;
        //        }

        //        // Handle Didomi pop-up initially
        //        DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

        //        int pageCount = 1;
        //        while (pageCount <= maxPages)
        //        {
        //            Console.WriteLine($"Scraping page {pageCount}...");

        //            try
        //            {
        //                // Recheck for Didomi pop-up on each page
        //                DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

        //                // Wait for at least 15 properties to load
        //                wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 15);

        //                // Parse current page
        //                var html = driver.PageSource;
        //                var htmlDocument = new HtmlDocument();
        //                htmlDocument.LoadHtml(html);

        //                // Scrape properties
        //                var propertyNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']");
        //                if (propertyNodes != null)
        //                {
        //                    // Collect all property URLs to avoid stale element issues
        //                    var propertyUrls = new List<string>();
        //                    foreach (var node in propertyNodes)
        //                    {
        //                        var urlNode = node.SelectSingleNode(".//a[@class='property-thumbnail-summary-link']");
        //                        var url = urlNode?.GetAttributeValue("href", "");
        //                        if (!string.IsNullOrEmpty(url))
        //                        {
        //                            propertyUrls.Add("https://www.centris.ca" + url);
        //                        }
        //                    }

        //                    // Iterate through each property URL
        //                    for (int i = 0; i < propertyUrls.Count; i++)
        //                    {
        //                        var propertyUrl = propertyUrls[i];
        //                        Console.WriteLine($"Navigating to property detail page {i + 1}/{propertyUrls.Count}: {propertyUrl}");

        //                        try
        //                        {
        //                            // Navigate to detail page
        //                            driver.Navigate().GoToUrl(propertyUrl);
        //                            System.Threading.Thread.Sleep(5000); // Wait for detail page to load
        //                            DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

        //                            // Wait for description section to load
        //                            wait.Until(d => d.FindElements(By.XPath("//div[@class='col-lg-12 description']")).Count > 0);

        //                            // Parse detail page
        //                            var detailHtml = driver.PageSource;
        //                            var detailDoc = new HtmlDocument();
        //                            detailDoc.LoadHtml(detailHtml);

        //                            var property = new Property
        //                            {
        //                                Meta_Content = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='name']")?.GetAttributeValue("content", ""),
        //                                Property_URl = propertyUrl,
        //                                PropertyImage = detailDoc.DocumentNode.SelectSingleNode("//img[@itemprop='image']")?.GetAttributeValue("src", ""),
        //                                MlsNumberNoStealth = detailDoc.DocumentNode.SelectSingleNode("//div[@id='MlsNumberNoStealth']//p")?.InnerText.Trim(),
        //                                PriceCurrency = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='priceCurrency']")?.GetAttributeValue("content", ""),
        //                                Price = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='price']")?.GetAttributeValue("content", ""),
        //                                Category = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='category']//div")?.InnerText.Trim(),
        //                                Address = detailDoc.DocumentNode.SelectNodes("//div[@class='address']//div") is HtmlNodeCollection addressNodes && addressNodes.Count >= 2
        //                                    ? $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}"
        //                                    : "",
        //                                Orgazination_Name = detailDoc.DocumentNode.SelectSingleNode("//p[@class='organisation-name']")?.InnerText.Trim(),
        //                                Amenities = new Dictionary<string, string>(),
        //                                Latetude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lat", ""),
        //                                Longitude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lng", "")
        //                            };

        //                            // Extract amenities from detail page
        //                            var descriptionNode = detailDoc.DocumentNode.SelectSingleNode("//div[@class='col-lg-12 description']");
        //                            if (descriptionNode != null)
        //                            {
        //                                // Teaser section (rooms, bedrooms, bathrooms, etc.)
        //                                var teaserNodes = descriptionNode.SelectNodes(".//div[@class='row teaser']//div[contains(@class, 'col-lg-3 col-sm-6')]");
        //                                if (teaserNodes != null)
        //                                {
        //                                    foreach (var teaserNode in teaserNodes)
        //                                    {
        //                                        var className = teaserNode.GetAttributeValue("class", "");
        //                                        var value = teaserNode.InnerText.Trim();
        //                                        if (className.Contains("piece") && !string.IsNullOrEmpty(value))
        //                                        {
        //                                            property.Amenities["Rooms"] = value;
        //                                        }
        //                                        else if (className.Contains("cac") && !string.IsNullOrEmpty(value))
        //                                        {
        //                                            property.Amenities["Bedrooms"] = value;
        //                                        }
        //                                        else if (className.Contains("sdb") && !string.IsNullOrEmpty(value))
        //                                        {
        //                                            property.Amenities["Bathrooms"] = value;
        //                                        }
        //                                        else if (className.Contains("lifestyle") && !string.IsNullOrEmpty(value))
        //                                        {
        //                                            var scoreNode = teaserNode.SelectSingleNode(".//span[@class='ll-match-score noAnimation']//span[@class='ll-score-color-default']");
        //                                            property.Amenities["LifestyleScore"] = scoreNode?.InnerText.Trim() ?? "?";
        //                                        }
        //                                    }
        //                                }

        //                                // Carac-container section (building style, year built, etc.)
        //                                var caracNodes = descriptionNode.SelectNodes(".//div[@class='row']//div[contains(@class, 'carac-container')]");
        //                                if (caracNodes != null)
        //                                {
        //                                    foreach (var caracNode in caracNodes)
        //                                    {
        //                                        var titleNode = caracNode.SelectSingleNode(".//div[@class='carac-title']");
        //                                        var valueNode = caracNode.SelectSingleNode(".//div[@class='carac-value']//span");
        //                                        if (titleNode != null && valueNode != null)
        //                                        {
        //                                            var title = titleNode.InnerText.Trim();
        //                                            var value = valueNode.InnerText.Trim();
        //                                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(value))
        //                                            {
        //                                                property.Amenities[title] = value;
        //                                            }
        //                                        }
        //                                    }
        //                                }

        //                                // Walkscore
        //                                var walkscoreNode = descriptionNode.SelectSingleNode(".//div[@class='walkscore']//span");
        //                                if (walkscoreNode != null)
        //                                {
        //                                    property.Amenities["WalkScore"] = walkscoreNode.InnerText.Trim();
        //                                }
        //                            }

        //                            properties.Add(property);
        //                            Console.WriteLine($"Scraped property {i + 1}/{propertyUrls.Count} from detail page.");
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Console.WriteLine($"Error scraping detail page {propertyUrl}: {ex.Message}");
        //                            File.WriteAllText($"detail_error_{i}_{timestamp}.html", driver.PageSource);
        //                        }

        //                        // Navigate back to the listing page
        //                        driver.Navigate().GoToUrl(baseUrl);
        //                        System.Threading.Thread.Sleep(5000);
        //                        DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);
        //                        wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 15);
        //                    }

        //                    Console.WriteLine($"Scraped {propertyUrls.Count} properties on page {pageCount}.");
        //                }
        //                else
        //                {
        //                    Console.WriteLine($"No properties found on page {pageCount}.");
        //                    File.WriteAllText($"page_{pageCount}_no_properties_{timestamp}.html", driver.PageSource);
        //                }

        //                // Check for next button
        //                IWebElement nextButton;
        //                try
        //                {
        //                    nextButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//ul[@class='pager']//li[contains(@class, 'next') and not(contains(@class, 'inactive'))]/a")));
        //                }
        //                catch (WebDriverTimeoutException)
        //                {
        //                    Console.WriteLine("No more pages to scrape (next button is inactive or not found).");
        //                    File.WriteAllText($"page_{pageCount}_no_next_button_{timestamp}.html", driver.PageSource);
        //                    break;
        //                }

        //                // Click the next button with retry logic
        //                bool clicked = false;
        //                for (int attempt = 1; attempt <= maxRetries; attempt++)
        //                {
        //                    Console.WriteLine($"Clicking next button (attempt {attempt}/{maxRetries})...");
        //                    try
        //                    {
        //                        // Scroll to ensure button is in view
        //                        driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", nextButton);
        //                        System.Threading.Thread.Sleep(2000);

        //                        // Try regular click
        //                        nextButton.Click();
        //                        clicked = true;
        //                        System.Threading.Thread.Sleep(10000); // Wait for page load
        //                        break;
        //                    }
        //                    catch (ElementClickInterceptedException ex)
        //                    {
        //                        Console.WriteLine($"Next button click intercepted: {ex.Message}");
        //                        // Recheck for Didomi pop-up
        //                        DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);
        //                        if (attempt == maxRetries)
        //                        {
        //                            Console.WriteLine("Max retries reached for clicking next button.");
        //                            File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
        //                            break;
        //                        }
        //                        // Try JavaScript click
        //                        try
        //                        {
        //                            driver.ExecuteScript("arguments[0].click();", nextButton);
        //                            clicked = true;
        //                            System.Threading.Thread.Sleep(10000);
        //                            break;
        //                        }
        //                        catch (Exception jsEx)
        //                        {
        //                            Console.WriteLine($"JavaScript click failed: {jsEx.Message}");
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine($"Failed to click next button: {ex.Message}");
        //                        File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
        //                        break;
        //                    }
        //                }

        //                if (!clicked)
        //                {
        //                    Console.WriteLine("Failed to click next button after retries.");
        //                    break;
        //                }

        //                pageCount++;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error scraping page {pageCount}: {ex.Message}");
        //                File.WriteAllText($"page_{pageCount}_error_{timestamp}.html", driver.PageSource);
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Selenium setup or initial navigation failed: {ex.Message}");
        //        File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
        //        return null;
        //    }
        //    finally
        //    {
        //        driver?.Quit();
        //    }

        //    // Print the properties
        //    foreach (var prop in properties)
        //    {
        //        Console.WriteLine("Property Details:");
        //        Console.WriteLine($"Meta Content: {prop.Meta_Content}");
        //        Console.WriteLine($"Property URL: {prop.Property_URl}");
        //        Console.WriteLine($"Property Image: {prop.PropertyImage}");
        //        Console.WriteLine($"MLS Number: {prop.MlsNumberNoStealth}");
        //        Console.WriteLine($"Price Currency: {prop.PriceCurrency}");
        //        Console.WriteLine($"Price: {prop.Price}");
        //        Console.WriteLine($"Category: {prop.Category}");
        //        Console.WriteLine($"Address: {prop.Address}");
        //        Console.WriteLine($"Organization Name: {prop.Orgazination_Name}");
        //        Console.WriteLine("Amenities:");
        //        foreach (var amenity in prop.Amenities)
        //        {
        //            Console.WriteLine($"  {amenity.Key}: {amenity.Value}");
        //        }
        //        Console.WriteLine($"Latitude: {prop.Latetude}");
        //        Console.WriteLine($"Longitude: {prop.Longitude}");
        //        Console.WriteLine(new string('-', 50));
        //    }

        //    Console.WriteLine($"Total properties scraped: {properties.Count}");

        //    // Save to JSON
        //    return properties;// File.WriteAllText($"properties_{timestamp}.json", JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true }));
        //}
        private async Task DismissDidomiPopupAsync(IWebDriver driver, WebDriverWait wait, int maxRetries, string timestamp)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var consentButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(
                        "//button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'accept') or contains(@class, 'didomi-button') or @id='didomi-notice-agree-button']"
                    )));
                    driver.ExecuteJavaScript("arguments[0].scrollIntoView({block: 'center'});", consentButton);
                    await Task.Delay(1000);
                    consentButton.Click();
                    await Task.Delay(2000);
                    if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup')]")).Count == 0)
                    {
                        Console.WriteLine("Didomi pop-up dismissed.");
                        return;
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine("No Didomi pop-up found.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error dismissing Didomi pop-up: {ex.Message}");
                }
            }

            // Final attempt with JavaScript
            try
            {
                driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
                await Task.Delay(2000);
                Console.WriteLine("Didomi pop-up dismissed via JavaScript.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JavaScript dismissal failed: {ex.Message}");
                File.WriteAllText($"didomi_error_{timestamp}.html", driver.PageSource);
            }
        }
        //private async Task DismissDidomiPopupAsync(IWebDriver driver, WebDriverWait wait, int maxRetries, string timestamp)
        //{
        //     for (int attempt = 1; attempt <= maxRetries; attempt++)
        //    {
        //        try
        //        {
        //            // Try clicking a consent button
        //            var consentButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(
        //                "//button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'accept') " +
        //                "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'agree') " +
        //                "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ok') " +
        //                "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'save') " +
        //                "or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'continue') " +
        //                "or contains(@class, 'didomi-button') or contains(@class, 'didomi-consent') or contains(@id, 'didomi-notice-agree-button') " +
        //                "or contains(@id, 'didomi-consent-popup-continue') or contains(@id, 'didomi-consent-popup-accept') or @aria-label='Accept'] " +
        //                "| //div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host']//button"
        //            )));
        //            if (consentButton != null)
        //            {
        //                Console.WriteLine($"Dismissing Didomi pop-up (attempt {attempt}/{maxRetries})...");
        //                try
        //                {
        //                    driver.ExecuteJavaScript("arguments[0].scrollIntoView({block: 'center'});", consentButton);
        //                    System.Threading.Thread.Sleep(2000);
        //                    consentButton.Click();
        //                    System.Threading.Thread.Sleep(3000); // Wait for pop-up to close
        //                    // Verify pop-up is gone
        //                    if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
        //                    {
        //                        Console.WriteLine("Didomi pop-up successfully dismissed.");
        //                        return;
        //                    }
        //                }
        //                catch (ElementClickInterceptedException)
        //                {
        //                    Console.WriteLine("Consent button click intercepted, trying JavaScript click...");
        //                    driver.ExecuteJavaScript("arguments[0].click();", consentButton);
        //                    System.Threading.Thread.Sleep(3000);
        //                    if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
        //                    {
        //                        Console.WriteLine("Didomi pop-up successfully dismissed via JavaScript.");
        //                        return;
        //                    }
        //                }
        //            }
        //        }
        //        catch (WebDriverTimeoutException)
        //        {
        //            Console.WriteLine($"No Didomi pop-up found or already dismissed on attempt {attempt}.");
        //            // Try JavaScript to accept all consents
        //            try
        //            {
        //                driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
        //                System.Threading.Thread.Sleep(3000);
        //                if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
        //                {
        //                    Console.WriteLine("Didomi pop-up dismissed via Didomi.setUserAgreeToAll().");
        //                    return;
        //                }
        //            }
        //            catch (Exception jsEx)
        //            {
        //                Console.WriteLine($"JavaScript consent failed: {jsEx.Message}");
        //            }
        //            // Try hiding the pop-up
        //            try
        //            {
        //                driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
        //                System.Threading.Thread.Sleep(2000);
        //                if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
        //                {
        //                    Console.WriteLine("Didomi pop-up hidden via CSS.");
        //                    return;
        //                }
        //            }
        //            catch (Exception jsEx)
        //            {
        //                Console.WriteLine($"CSS hide failed: {jsEx.Message}");
        //            }
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error handling Didomi pop-up on attempt {attempt}: {ex.Message}");
        //            File.WriteAllText($"didomi_error_attempt_{attempt}_{timestamp}.html", driver.PageSource);
        //        }
        //    }
        //    Console.WriteLine("Max retries reached for Didomi pop-up dismissal.");
        //    // Final attempt to disable Didomi pop-up via JavaScript
        //    try
        //    {
        //        driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
        //        driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
        //        System.Threading.Thread.Sleep(3000);
        //        if (driver.FindElements(By.XPath("//div[@id='didomi-popup' or contains(@class, 'didomi-popup') or @id='didomi-host' or contains(@class, 'didomi-consent-popup')]")).Count == 0)
        //        {
        //            Console.WriteLine("Didomi pop-up dismissed via final JavaScript.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Failed to dismiss Didomi pop-up after all attempts.");
        //            File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Final JavaScript consent failed: {ex.Message}");
        //        File.WriteAllText($"didomi_error_final_{timestamp}.html", driver.PageSource);
        //    }
        //}


    }
}
