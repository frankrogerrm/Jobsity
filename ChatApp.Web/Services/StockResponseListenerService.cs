using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Services
{
    public class StockResponseListenerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockResponseListenerService> _logger;

        public StockResponseListenerService(
            IServiceProvider serviceProvider,
            ILogger<StockResponseListenerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Response Listener Service is starting.");

            await Task.Run(() =>
            {
                using var scope = _serviceProvider.CreateScope();
                var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                rabbitMQService.StartListening(async (response) =>
                {
                    try
                    {
                        using var innerScope = _serviceProvider.CreateScope();
                        var innerChatService = innerScope.ServiceProvider.GetRequiredService<IChatService>();
                        var innerHubContext = innerScope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                        var message = await innerChatService.AddMessageAsync(
                            "StockBot",
                            response.Message,
                            response.ChatRoomId,
                            isBot: true
                        );

                        await innerHubContext.Clients.Group($"Room_{response.ChatRoomId}")
                            .SendAsync("ReceiveMessage", message.Username, message.Message, message.Timestamp, message.IsBot);

                        _logger.LogInformation($"Stock response processed: {response.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing stock response");
                    }
                });

                stoppingToken.WaitHandle.WaitOne();
            }, stoppingToken);

            _logger.LogInformation("Stock Response Listener Service is stopping.");
        }
    }
}
