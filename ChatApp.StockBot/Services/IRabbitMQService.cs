using ChatApp.Shared.Models;

namespace ChatApp.StockBot.Services
{
    public interface IRabbitMQService
    {
        void PublishStockResponse(StockQuoteResponse response);
    }
}
