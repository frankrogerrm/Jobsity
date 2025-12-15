using ChatApp.Shared.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ChatApp.StockBot.Services
{
    public class StockQuoteService : IStockQuoteService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StockQuoteService> _logger;

        public StockQuoteService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<StockQuoteService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<StockQuoteResponse> GetStockQuoteAsync(StockCommand command)
        {
            try
            {
                var baseUrl = _configuration["StooqApi:BaseUrl"];
                var queryFormat = _configuration["StooqApi:QueryFormat"];
                var url = $"{baseUrl}{string.Format(queryFormat!, command.StockCode)}";

                _logger.LogInformation($"Fetching stock quote from: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var csvContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"CSV Content: {csvContent}");

                var stockData = ParseCsv(csvContent);

                if (stockData == null || stockData.Close <= 0)
                {
                    return new StockQuoteResponse
                    {
                        StockCode = command.StockCode,
                        ChatRoomId = command.ChatRoomId,
                        IsError = true,
                        ErrorMessage = $"Stock {command.StockCode} not found or has invalid data.",
                        Message = $"Unable to retrieve quote for {command.StockCode}. Please check the stock code."
                    };
                }

                return new StockQuoteResponse
                {
                    StockCode = stockData.Symbol,
                    Price = stockData.Close,
                    ChatRoomId = command.ChatRoomId,
                    IsError = false,
                    Message = $"{stockData.Symbol} quote is ${stockData.Close.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} per share"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error fetching stock quote for {command.StockCode}");
                return CreateErrorResponse(command, "Network error while fetching stock data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing stock command for {command.StockCode}");
                return CreateErrorResponse(command, "An error occurred while processing your request.");
            }
        }

        private StockData? ParseCsv(string csvContent)
        {
            try
            {
                using var reader = new StringReader(csvContent);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null
                });

                var records = csv.GetRecords<StockData>().ToList();
                return records.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV");
                return null;
            }
        }

        private StockQuoteResponse CreateErrorResponse(StockCommand command, string errorMessage)
        {
            return new StockQuoteResponse
            {
                StockCode = command.StockCode,
                ChatRoomId = command.ChatRoomId,
                IsError = true,
                ErrorMessage = errorMessage,
                Message = $"Error retrieving quote for {command.StockCode}: {errorMessage}"
            };
        }

        private class StockData
        {
            public string Symbol { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public long Volume { get; set; }
        }
    }
}
