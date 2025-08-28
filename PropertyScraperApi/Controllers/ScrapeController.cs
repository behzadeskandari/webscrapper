using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MassTransit;
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
        private readonly IPublishEndpoint _publishEndpoint;

        public ScrapeController(IScraperService scraperService, IPublishEndpoint publishEndpoint)
        {
            _scraperService = scraperService;
            _publishEndpoint = publishEndpoint;
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


        [HttpPost]
        public async Task<IActionResult> Scrapes([FromBody] ScrapeRequest request)
        {
            for (int page = 1; page <= (request.MaxPages ?? 250); page++)
            {
                await _publishEndpoint.Publish(new ScrapePageCommand { PageNumber = page });
            }
            return Ok(new { Message = "Scraping tasks queued." });
        }
    }

}
