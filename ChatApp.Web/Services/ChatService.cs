using ChatApp.Shared.Models;
using ChatApp.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ChatApp.Web.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Regex StockCommandRegex = new(@"^/stock=([a-zA-Z0-9\.]+)$", RegexOptions.IgnoreCase);

        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessage>> GetLastMessagesAsync(int chatRoomId, int count = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(string username, string message, int chatRoomId, bool isBot = false)
        {
            var chatMessage = new ChatMessage
            {
                Username = username,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsBot = isBot,
                ChatRoomId = chatRoomId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return chatMessage;
        }

        public async Task<List<ChatRoom>> GetChatRoomsAsync()
        {
            return await _context.ChatRooms
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public bool IsStockCommand(string message)
        {
            return StockCommandRegex.IsMatch(message.Trim());
        }

        public string? ExtractStockCode(string message)
        {
            var match = StockCommandRegex.Match(message.Trim());
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
