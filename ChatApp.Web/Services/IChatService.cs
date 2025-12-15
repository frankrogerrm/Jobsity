using ChatApp.Shared.Models;

namespace ChatApp.Web.Services
{
    public interface IChatService
    {
        Task<List<ChatMessage>> GetLastMessagesAsync(int chatRoomId, int count = 50);
        Task<ChatMessage> AddMessageAsync(string username, string message, int chatRoomId, bool isBot = false);
        Task<List<ChatRoom>> GetChatRoomsAsync();
        bool IsStockCommand(string message);
        string? ExtractStockCode(string message);
    }
}
