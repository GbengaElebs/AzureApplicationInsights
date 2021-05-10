
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationInsightsDemo.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using System.Data;
using Dapper;
using MySqlConnector;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ApplicationInsightsDemo.Controllers
{
    //[ApiController]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string connString;
        private readonly string host;
        private readonly string port;
        private readonly string usersDataBase;
        private TelemetryClient _telemetryClient;
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, TelemetryClient telemetry)
        {
            _logger = logger;
            _configuration = configuration;
            _logger = logger;
            _telemetryClient = telemetry;
            host = "localhost";
            port = "3306";
            var password = _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration.GetConnectionString("MYSQL_USER");
            usersDataBase = _configuration.GetConnectionString("MYSQL_DATABASE");
            connString = $"server={host}; userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        [Route("api/CallApi")]
        [HttpGet]
        public async Task<IActionResult> CallExternalApi(string button)
        {
            try
            {
                string apiResponse = string.Empty;
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync("https://api.blockchain.com/v3/exchange/tickers"))
                    {
                        apiResponse = await response.Content.ReadAsStringAsync();
                    }
                }
                var returnObj = JsonConvert.DeserializeObject<List<ApiResponse>>(apiResponse);

                if (!string.IsNullOrEmpty(button))
                {
                    TempData["ButtonValue"] = string.Format("Price Of Bitcoin is {0} USD", returnObj.FirstOrDefault(x => x.symbol == "BTC-USD").price_24h);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.ToString());
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> DataBaseCall()
        {
            string result = string.Empty;
            try
            {
                // Establish an operation context and associated telemetry item:
                using (var operation = _telemetryClient.StartOperation<DependencyTelemetry>("MySqlDb"))
                {
                    string query = @"SELECT UserName FROM Users";
                    operation.Telemetry.Data = query;
                    operation.Telemetry.Target = $"server={host}:{port} - DB:{usersDataBase}";
                    //sending details of the database call Application insights
                    using (var connection = new MySqlConnection(connString))
                    {
                        result = await connection.QueryFirstOrDefaultAsync<string>(query, CommandType.Text);
                    }
                    if (!string.IsNullOrEmpty(result))
                    {
                        operation.Telemetry.Success = true;
                        //informing Application Insights that the db call is successful
                    }
                    _telemetryClient.StopOperation(operation);
                }
                TempData["DataBaseButtonValue"] = string.Format("Logged In User is {0}", result);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }
            finally
            {
            }
            return RedirectToAction("Index");
        }
        public IActionResult Exception()
        {
            try
            {
                throw new HttpRequestException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["ExceptionButtonValue"] = ex.Message;
            }
            finally
            {
            }
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
