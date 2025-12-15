using ChatApp.Shared.Models;

namespace ChatApp.StockBot.Services
{
    public interface IStockQuoteService
    {
        Task<StockQuoteResponse> GetStockQuoteAsync(StockCommand command);
    }
}
