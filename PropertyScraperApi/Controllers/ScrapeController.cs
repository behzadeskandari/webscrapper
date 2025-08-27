using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using PropertyScraperApi.Service;
using SeleniumExtras.WaitHelpers;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using webscrapper;
using UndetectedChromeDriver = SeleniumUndetectedChromeDriver.UndetectedChromeDriver;

namespace PropertyScraperApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScrapeController : ControllerBase
    {

        private readonly IScraperService _scraperService;

        public ScrapeController(IScraperService scraperService)
        {
            _scraperService = scraperService;
        }

        [HttpPost]
        public async Task<IActionResult> Scrape([FromBody] ScrapeRequest request)
        {
            try
            {
                var properties = await _scraperService.ScrapePropertiesAsync(request.MaxPages ?? 250);
                return Ok(properties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Scraping failed: {ex.Message}" });
            }
        }

    }

}
