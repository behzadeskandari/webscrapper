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
using webscrapper;

namespace PropertyScraperApi.Service
{
    public interface IScraperService
    {
        Task<List<Property>> ScrapePropertiesAsync(int maxPages);
    }


    public class ScraperService : IScraperService
    {
        public async Task<List<Property>> ScrapePropertiesAsync(int maxPages)
        {
            var properties = new List<Property>();
            const int maxRetries = 5;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Setup WebDriverManager
            new DriverManager().SetUpDriver(new ChromeConfig());
            var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig());

            // Configure ChromeOptions
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
            using var driver = UndetectedChromeDriver.Create(
                options: chromeOptions,
                driverExecutablePath: chromeDriverPath,
                browserExecutablePath: null
            );

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            try
            {
                string baseUrl = "https://www.centris.ca/en/properties~for-sale?uc=1";
                driver.Navigate().GoToUrl(baseUrl);
                await Task.Delay(10000); // Wait for Cloudflare

                if (driver.PageSource.Contains("cf-captcha") || driver.PageSource.Contains("Access denied"))
                {
                    File.WriteAllText($"initial_error_{timestamp}.html", driver.PageSource);
                    throw new Exception("Cloudflare CAPTCHA or access denied detected.");
                }

                // Handle Didomi pop-up initially
                await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

                int pageCount = 1;
                while (pageCount <= maxPages)
                {
                    Console.WriteLine($"Scraping page {pageCount}...");

                    // Recheck for Didomi pop-up
                    await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);

                    // Wait for properties to load
                    wait.Until(d => d.FindElements(By.XPath("//div[@class='property-thumbnail-item thumbnailItem col-12 col-sm-6 col-md-4 col-lg-3']")).Count >= 15);

                    // Parse current page
                    var html = driver.PageSource;
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);

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
                        Console.WriteLine("No more pages to scrape.");
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
                            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", nextButton);
                            await Task.Delay(2000);
                            nextButton.Click();
                            clicked = true;
                            await Task.Delay(10000);
                            break;
                        }
                        catch (ElementClickInterceptedException ex)
                        {
                            Console.WriteLine($"Next button click intercepted: {ex.Message}");
                            await DismissDidomiPopupAsync(driver, wait, maxRetries, timestamp);
                            if (attempt == maxRetries)
                            {
                                Console.WriteLine("Max retries reached for clicking next button.");
                                File.WriteAllText($"page_{pageCount}_click_error_{timestamp}.html", driver.PageSource);
                                break;
                            }
                            try
                            {
                                driver.ExecuteScript("arguments[0].click();", nextButton);
                                clicked = true;
                                await Task.Delay(10000);
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
            }
            finally
            {
                driver?.Quit();
            }

            // Save to JSON file
            File.WriteAllText($"properties_{timestamp}.json", JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true }));
            return properties;
        }

        private async Task DismissDidomiPopupAsync(IWebDriver driver, WebDriverWait wait, int maxRetries, string timestamp)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
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
                            await Task.Delay(2000);
                            consentButton.Click();
                            await Task.Delay(3000);
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
                            await Task.Delay(3000);
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
                    try
                    {
                        driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
                        await Task.Delay(3000);
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
                    try
                    {
                        driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
                        await Task.Delay(2000);
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
            try
            {
                driver.ExecuteJavaScript("if (window.Didomi) { window.Didomi.setUserAgreeToAll(); }");
                driver.ExecuteJavaScript("document.querySelectorAll('#didomi-popup, .didomi-popup-backdrop').forEach(el => el.style.display = 'none');");
                await Task.Delay(3000);
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
