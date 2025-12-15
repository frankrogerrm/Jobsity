using ChatApp.Shared.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ChatApp.StockBot.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _stockResponseQueue;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _stockResponseQueue = configuration["RabbitMQ:StockResponseQueue"] ?? "stock_responses";

            _channel.QueueDeclare(
                queue: _stockResponseQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("RabbitMQ Service initialized for Bot");
        }

        public void PublishStockResponse(StockQuoteResponse response)
        {
            try
            {
                var message = JsonSerializer.Serialize(response);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _stockResponseQueue,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation($"Published stock response: {response.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing stock response");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("RabbitMQ Service disposed");
        }
    }
}
