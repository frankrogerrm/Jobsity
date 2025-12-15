using ChatApp.Shared.Models;

namespace ChatApp.Web.Services
{
    public interface IRabbitMQService
    {
        void PublishStockCommand(StockCommand command);
        void StartListening(Action<StockQuoteResponse> onMessageReceived);
        void StopListening();
    }
}
