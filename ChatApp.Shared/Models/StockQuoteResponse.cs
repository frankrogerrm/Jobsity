namespace ChatApp.Shared.Models
{
    public class StockQuoteResponse
    {
        public string StockCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
