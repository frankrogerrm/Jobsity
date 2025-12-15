using ChatApp.Shared.Models;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs
{

    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            IRabbitMQService rabbitMQService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        private string GetUsername()
        {
            return Context.User?.Identity?.Name ?? "Anonymous";
        }

        public async Task JoinRoom(int chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{chatRoomId}");

            _logger.LogInformation($"User {GetUsername()} joined room {chatRoomId}");

            var messages = await _chatService.GetLastMessagesAsync(chatRoomId);
            foreach (var msg in messages)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", msg.Username, msg.Message, msg.Timestamp, msg.IsBot);
            }
        }

        public async Task LeaveRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{chatRoomId}");
            _logger.LogInformation($"User {GetUsername()} left room {chatRoomId}");
        }

        public async Task SendMessage(string message, int chatRoomId)
        {
            var username = GetUsername();

            try
            {
                if (_chatService.IsStockCommand(message))
                {
                    var stockCode = _chatService.ExtractStockCode(message);

                    if (!string.IsNullOrEmpty(stockCode))
                    {
                        var stockCommand = new StockCommand
                        {
                            StockCode = stockCode,
                            Username = username,
                            ChatRoomId = chatRoomId,
                            RequestedAt = DateTime.UtcNow
                        };

                        _rabbitMQService.PublishStockCommand(stockCommand);

                        _logger.LogInformation($"Stock command published: {stockCode} by {username}");

                        await Clients.Caller.SendAsync("CommandProcessed", $"Processing stock quote for {stockCode}...");
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("Error", "Invalid stock command format. Use /stock=CODE");
                    }
                }
                else
                {
                    var chatMessage = await _chatService.AddMessageAsync(username, message, chatRoomId);

                    await Clients.Group($"Room_{chatRoomId}")
                        .SendAsync("ReceiveMessage", chatMessage.Username, chatMessage.Message, chatMessage.Timestamp, chatMessage.IsBot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}, User: {GetUsername()}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, User: {GetUsername()}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}