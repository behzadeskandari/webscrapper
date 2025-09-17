using System.Text.Json;
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
                new DriverManager().SetUpDriver(new ChromeConfig());
                var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());
                //var chromeDriverPath = Environment.OSVersion.Platform == PlatformID.Win32NT
                //    ? "chromedriver.exe"
                //    : "/usr/local/bin/chromedriver";
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

                            //                    var property = new Property
                            //                    {
                            //                        Meta_Content = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='name']")?.GetAttributeValue("content", ""),
                            //                        Property_URl = propertyUrl,
                            //                        PropertyImage = detailDoc.DocumentNode.SelectSingleNode("//img[@itemprop='image']")?.GetAttributeValue("src", ""),
                            //                        MlsNumberNoStealth = detailDoc.DocumentNode.SelectSingleNode("//div[@id='MlsNumberNoStealth']//p")?.InnerText.Trim(),
                            //                        PriceCurrency = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='priceCurrency']")?.GetAttributeValue("content", ""),
                            //                        Price = detailDoc.DocumentNode.SelectSingleNode("//meta[@itemprop='price']")?.GetAttributeValue("content", ""),
                            //                        Category = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='category']//div")?.InnerText.Trim(),
                            //                        Address = detailDoc.DocumentNode.SelectNodes("//div[@class='address']//div") is HtmlNodeCollection addressNodes && addressNodes.Count >= 2
                            //? $"{addressNodes[0]?.InnerText.Trim()}, {addressNodes[1]?.InnerText.Trim()}"
                            //: "",
                            //                        Orgazination_Name = detailDoc.DocumentNode.SelectSingleNode("//p[@class='organisation-name']")?.InnerText.Trim(),
                            //                        Amenities = new Dictionary<string, string>(),
                            //                        Latetude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lat", ""),
                            //                        Longitude = detailDoc.DocumentNode.SelectSingleNode("//span[@class='ll-match-score noAnimation']")?.GetAttributeValue("data-lng", ""),
                            //                        Description = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='description']")?.InnerText.Trim(),
                            //                        FinancialDetails = new Dictionary<string, string>(),
                            //                        BrokerNames = new List<string>(),
                            //                        BrokerPhones = new List<string>(),
                            //                        PhotoCount = int.TryParse(detailDoc.DocumentNode.SelectSingleNode("//button[contains(@class, 'photo-btn') and contains(., 'fa-camera')]")?.InnerText.Replace(" ", "").Replace("\n", "").Split(new[] { "<i" }, StringSplitOptions.None)[0], out var count) ? count : 0,
                            //                        AdditionalPhotoUrls = new List<string>()
                            //                    };

                            //                    // Extract multiple brokers
                            //                    var brokerNodes = detailDoc.DocumentNode.SelectNodes("//div[@class='property-summary-item__brokers-content']//div[@itemtype='https://schema.org/RealEstateAgent']");
                            //                    if (brokerNodes != null)
                            //                    {
                            //                        foreach (var brokerNode in brokerNodes)
                            //                        {
                            //                            var brokerName = brokerNode.SelectSingleNode(".//h1[@class='broker-info__broker-title h5 mb-0']")?.InnerText.Trim();
                            //                            if (!string.IsNullOrEmpty(brokerName))
                            //                            {
                            //                                property.BrokerNames.Add(brokerName);
                            //                            }

                            //                            var phoneNodes = brokerNode.SelectNodes(".//a[@itemprop='telephone']");
                            //                            if (phoneNodes != null)
                            //                            {
                            //                                foreach (var phoneNode in phoneNodes)
                            //                                {
                            //                                    var phone = phoneNode.GetAttributeValue("content", "").Trim();
                            //                                    if (!string.IsNullOrEmpty(phone) && !property.BrokerPhones.Contains(phone))
                            //                                    {
                            //                                        property.BrokerPhones.Add(phone);
                            //                                    }
                            //                                }
                            //                            }
                            //                        }
                            //                    }

                            //                    // Extract amenities (simplified)
                            //                    var descriptionNode = detailDoc.DocumentNode.SelectSingleNode("//div[@class='col-lg-12 description']");
                            //                    if (descriptionNode != null)
                            //                    {
                            //                        var teaserNodes = descriptionNode.SelectNodes(".//div[@class='row teaser']//div[contains(@class, 'col-lg-3 col-sm-6')]");
                            //                        if (teaserNodes != null)
                            //                        {
                            //                            foreach (var node in teaserNodes)
                            //                            {
                            //                                var className = node.GetAttributeValue("class", "");
                            //                                var value = node.InnerText.Trim();
                            //                                if (className.Contains("piece")) property.Amenities["Rooms"] = value;
                            //                                else if (className.Contains("cac")) property.Amenities["Bedrooms"] = value;
                            //                                else if (className.Contains("sdb")) property.Amenities["Bathrooms"] = value;
                            //                                else if (className.Contains("lifestyle"))
                            //                                    property.Amenities["LifestyleScore"] = node.SelectSingleNode(".//span[@class='ll-score-color-default']")?.InnerText.Trim() ?? "?";
                            //                            }
                            //                        }

                            //                        var caracNodes = descriptionNode.SelectNodes(".//div[@class='row']//div[contains(@class, 'carac-container')]");
                            //                        if (caracNodes != null)
                            //                        {
                            //                            foreach (var node in caracNodes)
                            //                            {
                            //                                var title = node.SelectSingleNode(".//div[@class='carac-title']")?.InnerText.Trim();
                            //                                var value = node.SelectSingleNode(".//div[@class='carac-value']//span")?.InnerText.Trim();
                            //                                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(value))
                            //                                    property.Amenities[title] = value;
                            //                            }
                            //                        }

                            //                        var walkscoreNode = descriptionNode.SelectSingleNode(".//div[@class='walkscore']//span");
                            //                        if (walkscoreNode != null)
                            //                            property.Amenities["WalkScore"] = walkscoreNode.InnerText.Trim();
                            //                    }
                            //                    // Extract financial details
                            //                    var financialTables = detailDoc.DocumentNode.SelectNodes("//div[@class='financial-details-tables']//table[@class='table']");
                            //                    if (financialTables != null)
                            //                    {
                            //                        foreach (var table in financialTables)
                            //                        {
                            //                            var tableTitle = table.SelectSingleNode(".//th[@class='col pl-0 financial-details-table-title']")?.InnerText.Trim();
                            //                            if (string.IsNullOrEmpty(tableTitle)) continue;

                            //                            // Determine if the table is yearly or monthly
                            //                            var tableClass = table.GetAttributeValue("class", "");
                            //                            var period = tableClass.Contains("financial-details-table-yearly") ? "Yearly" :
                            //                                         tableClass.Contains("financial-details-table-monthly") ? "Monthly" : "";

                            //                            // Extract table rows
                            //                            var rows = table.SelectNodes(".//tbody/tr");
                            //                            if (rows != null)
                            //                            {
                            //                                foreach (var row in rows)
                            //                                {
                            //                                    var cells = row.SelectNodes("td");
                            //                                    if (cells?.Count == 2)
                            //                                    {
                            //                                        var key = string.IsNullOrEmpty(period) ? $"{tableTitle} - {cells[0].InnerText.Trim()}" :
                            //                                                  $"{tableTitle} - {cells[0].InnerText.Trim()} ({period})";
                            //                                        var value = cells[1].InnerText.Trim();
                            //                                        property.FinancialDetails[key] = value;
                            //                                    }
                            //                                }
                            //                            }

                            //                            // Extract total
                            //                            var totalRow = table.SelectSingleNode(".//tfoot/tr[@class='col pl-0 financial-details-table-total']");
                            //                            if (totalRow != null)
                            //                            {
                            //                                var totalCells = totalRow.SelectNodes("td");
                            //                                if (totalCells?.Count == 2)
                            //                                {
                            //                                    var key = string.IsNullOrEmpty(period) ? $"{tableTitle} - Total" :
                            //                                              $"{tableTitle} - Total ({period})";
                            //                                    var value = totalCells[1].InnerText.Trim();
                            //                                    property.FinancialDetails[key] = value;
                            //                                }
                            //                            }
                            //                        }
                            //                    }
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
                                Description = detailDoc.DocumentNode.SelectSingleNode("//div[@itemprop='description']")?.InnerText.Trim(),
                                FinancialDetails = new Dictionary<string, string>(),
                                BrokerNames = new List<string>(),
                                BrokerPhones = new List<string>(),
                                PhotoCount = int.TryParse(detailDoc.DocumentNode.SelectSingleNode("//button[contains(@class, 'photo-btn') and contains(., 'fa-camera')]")?.InnerText.Replace(" ", "").Replace("\n", "").Split(new[] { "<i" }, StringSplitOptions.None)[0], out var count) ? count : 0,
                                AdditionalPhotoUrls = new List<string>()
                            };

                            // Extract multiple brokers
                            var brokerNodes = detailDoc.DocumentNode.SelectNodes("//div[@class='property-summary-item__brokers-content']//div[@itemtype='https://schema.org/RealEstateAgent']");
                            if (brokerNodes != null)
                            {
                                foreach (var brokerNode in brokerNodes)
                                {
                                    var brokerName = brokerNode.SelectSingleNode(".//h1[@class='broker-info__broker-title h5 mb-0']")?.InnerText.Trim();
                                    if (!string.IsNullOrEmpty(brokerName))
                                    {
                                        property.BrokerNames.Add(brokerName);
                                    }

                                    var phoneNodes = brokerNode.SelectNodes(".//a[@itemprop='telephone']");
                                    if (phoneNodes != null)
                                    {
                                        foreach (var phoneNode in phoneNodes)
                                        {
                                            var phone = phoneNode.GetAttributeValue("content", "").Trim();
                                            if (!string.IsNullOrEmpty(phone) && !property.BrokerPhones.Contains(phone))
                                            {
                                                property.BrokerPhones.Add(phone);
                                            }
                                        }
                                    }
                                }
                            }

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

                                // Check property type
                                var propertyTypeNode = detailDoc.DocumentNode.SelectSingleNode("//h1[@itemprop='category']/span[@data-id='PageTitle']");
                                var propertyType = propertyTypeNode?.InnerText.Trim() ?? "Unknown";
                                Console.WriteLine($"Property type: {propertyType} for property {propertyUrl}");

                                // Ensure financial details section is loaded (if applicable)
                                bool hasFinancialSection = false;
                                try
                                {
                                    wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15)); // Increased timeout to 15 seconds
                                    wait.Until(d => d.FindElements(By.XPath("//div[@class='financial-details-tables']")).Count > 0);
                                    Console.WriteLine($"Financial details section found for property {propertyUrl}");
                                    hasFinancialSection = true;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"No financial details section found or timed out for property {propertyUrl}: {ex.Message}");
                                    File.WriteAllText($"financial_tables_wait_error_{i}_{timestamp}.html", driver.PageSource);
                                }

                                // Proceed with financial details extraction only if section exists
                                if (hasFinancialSection)
                                {
                                    // Toggle all tables to ensure visibility
                                    try
                                    {
                                        driver.ExecuteJavaScript("if (typeof toggleTables === 'function') { toggleTables(); }");
                                        await Task.Delay(2000); // Wait for tables to render
                                        Console.WriteLine($"Executed toggleTables() for property {propertyUrl}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error executing toggleTables(): {ex.Message}");
                                    }

                                    // Alternatively, click the "See all financial details" link if present
                                    try
                                    {
                                        var toggleLink = driver.FindElements(By.Id("toggleLink")).FirstOrDefault();
                                        if (toggleLink != null && toggleLink.Text.Contains("See all financial details"))
                                        {
                                            toggleLink.Click();
                                            await Task.Delay(2000); // Wait for tables to render
                                            Console.WriteLine($"Clicked 'See all financial details' link for property {propertyUrl}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error clicking toggle link: {ex.Message}");
                                    }

                                    // Reload page source after toggling
                                    var detailHtmls = driver.PageSource;
                                    detailDoc.LoadHtml(detailHtmls);
                                    File.WriteAllText($"financial_tables_post_toggle_{i}_{timestamp}.html", detailHtml);

                                    // Extract financial details
                                    var financialTables = detailDoc.DocumentNode.SelectNodes("//div[@class='financial-details-tables']//table[@class='table']");
                                    if (financialTables != null)
                                    {
                                        Console.WriteLine($"Found {financialTables.Count} financial tables for property {propertyUrl}");
                                        foreach (var table in financialTables)
                                        {
                                            var tableTitleNode = table.SelectSingleNode(".//th[@class='col pl-0 financial-details-table-title']");
                                            var tableTitle = tableTitleNode?.InnerText.Trim();
                                            if (string.IsNullOrEmpty(tableTitle))
                                            {
                                                Console.WriteLine($"Skipping table with missing title in {propertyUrl}");
                                                continue;
                                            }

                                            // Determine if the table is yearly or monthly
                                            var parentDiv = table.SelectSingleNode("ancestor::div[contains(@class, 'financial-details-table-')][1]");
                                            var period = parentDiv.GetAttributeValue("class", "").Contains("financial-details-table-yearly") ? "Yearly" :
                                                         parentDiv.GetAttributeValue("class", "").Contains("financial-details-table-monthly") ? "Monthly" : "";

                                            // Extract table rows
                                            var rows = table.SelectNodes(".//tbody/tr");
                                            if (rows != null)
                                            {
                                                foreach (var row in rows)
                                                {
                                                    var cells = row.SelectNodes("td");
                                                    if (cells?.Count == 2)
                                                    {
                                                        var key = string.IsNullOrEmpty(period) ? $"{tableTitle} - {cells[0].InnerText.Trim()}" :
                                                                  $"{tableTitle} - {cells[0].InnerText.Trim()} ({period})";
                                                        var value = cells[1].InnerText.Trim();
                                                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                                        {
                                                            property.FinancialDetails[key] = value;
                                                            Console.WriteLine($"Added financial detail: {key} = {value}");
                                                        }
                                                    }
                                                }
                                            }

                                            // Extract total
                                            var totalRow = table.SelectSingleNode(".//tfoot/tr[@class='col pl-0 financial-details-table-total']");
                                            if (totalRow != null)
                                            {
                                                var totalCells = totalRow.SelectNodes("td");
                                                if (totalCells?.Count == 2)
                                                {
                                                    var key = string.IsNullOrEmpty(period) ? $"{tableTitle} - Total" :
                                                              $"{tableTitle} - Total ({period})";
                                                    var value = totalCells[1].InnerText.Trim();
                                                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                                    {
                                                        property.FinancialDetails[key] = value;
                                                        Console.WriteLine($"Added financial total: {key} = {value}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"No financial tables found in DOM for property {propertyUrl}");
                                        File.WriteAllText($"financial_tables_error_{i}_{timestamp}.html", driver.PageSource);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Skipping financial details extraction for property {propertyUrl} (section not found)");
                                }
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


    }
}
