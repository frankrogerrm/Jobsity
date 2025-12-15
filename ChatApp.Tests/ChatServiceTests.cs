using ChatApp.Web.Data;
using ChatApp.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Tests
{
    public class ChatServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _chatService = new ChatService(_context);

            // Seed test data
            _context.ChatRooms.Add(new ChatApp.Shared.Models.ChatRoom
            {
                Id = 1,
                Name = "Test Room",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task AddMessageAsync_ShouldAddMessage()
        {
            // Arrange
            var username = "TestUser";
            var message = "Hello World";
            var chatRoomId = 1;

            // Act
            var result = await _chatService.AddMessageAsync(username, message, chatRoomId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(message, result.Message);
            Assert.Equal(chatRoomId, result.ChatRoomId);
            Assert.False(result.IsBot);
        }

        [Fact]
        public async Task GetLastMessagesAsync_ShouldReturnLast50Messages()
        {
            // Arrange
            for (int i = 0; i < 60; i++)
            {
                await _chatService.AddMessageAsync($"User{i}", $"Message {i}", 1);
            }

            // Act
            var messages = await _chatService.GetLastMessagesAsync(1, 50);

            // Assert
            Assert.Equal(50, messages.Count);
            Assert.Equal("Message 10", messages.First().Message);
            Assert.Equal("Message 59", messages.Last().Message);
        }

        [Theory]
        [InlineData("/stock=AAPL.US", true)]
        [InlineData("/stock=aapl.us", true)]
        [InlineData("/STOCK=MSFT.US", true)]
        [InlineData("Hello", false)]
        [InlineData("/stock=", false)]
        [InlineData("stock=AAPL.US", false)]
        public void IsStockCommand_ShouldIdentifyCommands(string message, bool expected)
        {
            // Act
            var result = _chatService.IsStockCommand(message);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/stock=AAPL.US", "AAPL.US")]
        [InlineData("/stock=msft.us", "msft.us")]
        [InlineData("/STOCK=GOOG", "GOOG")]
        [InlineData("Hello", null)]
        public void ExtractStockCode_ShouldExtractCorrectCode(string message, string? expected)
        {
            // Act
            var result = _chatService.ExtractStockCode(message);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task AddMessageAsync_WithBotFlag_ShouldMarkAsBot()
        {
            // Arrange
            var message = "AAPL.US quote is $150.00 per share";

            // Act
            var result = await _chatService.AddMessageAsync("StockBot", message, 1, isBot: true);

            // Assert
            Assert.True(result.IsBot);
            Assert.Equal("StockBot", result.Username);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
