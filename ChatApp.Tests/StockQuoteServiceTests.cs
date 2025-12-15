using ChatApp.Shared.Models;
using ChatApp.StockBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace ChatApp.Tests
{
    public class StockQuoteServiceTests
    {
        private readonly Mock<ILogger<StockQuoteService>> _loggerMock;
        private readonly IConfiguration _configuration;

        public StockQuoteServiceTests()
        {
            _loggerMock = new Mock<ILogger<StockQuoteService>>();

            var configDictionary = new Dictionary<string, string>
        {
            {"StooqApi:BaseUrl", "https://stooq.com/q/l/"},
            {"StooqApi:QueryFormat", "?s={0}&f=sd2t2ohlcv&h&e=csv"}
        };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDictionary!)
                .Build();
        }

        [Fact]
        public async Task GetStockQuoteAsync_WithValidResponse_ReturnsSuccessResponse()
        {
            // Arrange
            var csvResponse = @"Symbol,Date,Time,Open,High,Low,Close,Volume
AAPL.US,2024-01-15,22:00:00,150.5,152.0,149.5,151.25,50000000";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(csvResponse)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            var service = new StockQuoteService(httpClient, _configuration, _loggerMock.Object);

            var command = new StockCommand
            {
                StockCode = "AAPL.US",
                Username = "TestUser",
                ChatRoomId = 1,
                RequestedAt = DateTime.UtcNow
            };

            // Act
            var result = await service.GetStockQuoteAsync(command);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("AAPL.US", result.StockCode);
            Assert.Equal(151.25m, result.Price);
            Assert.Contains("$151.25", result.Message);
        }

        [Fact]
        public async Task GetStockQuoteAsync_WithInvalidData_ReturnsErrorResponse()
        {
            // Arrange
            var csvResponse = @"Symbol,Date,Time,Open,High,Low,Close,Volume
N/D,N/D,N/D,N/D,N/D,N/D,N/D,N/D";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(csvResponse)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            var service = new StockQuoteService(httpClient, _configuration, _loggerMock.Object);

            var command = new StockCommand
            {
                StockCode = "INVALID",
                Username = "TestUser",
                ChatRoomId = 1,
                RequestedAt = DateTime.UtcNow
            };

            // Act
            var result = await service.GetStockQuoteAsync(command);

            // Assert
            Assert.True(result.IsError);
            Assert.Contains("Unable to retrieve", result.Message);
        }

        [Fact]
        public async Task GetStockQuoteAsync_WithHttpError_ReturnsErrorResponse()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            var service = new StockQuoteService(httpClient, _configuration, _loggerMock.Object);

            var command = new StockCommand
            {
                StockCode = "TEST.US",
                Username = "TestUser",
                ChatRoomId = 1,
                RequestedAt = DateTime.UtcNow
            };

            // Act
            var result = await service.GetStockQuoteAsync(command);

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
