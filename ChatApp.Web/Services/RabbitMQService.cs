using ChatApp.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatApp.Web.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _stockCommandQueue;
        private readonly string _stockResponseQueue;
        private EventingBasicConsumer? _consumer;

        public RabbitMQService(IConfiguration configuration)
        {
            _configuration = configuration;

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _stockCommandQueue = _configuration["RabbitMQ:StockCommandQueue"] ?? "stock_commands";
            _stockResponseQueue = _configuration["RabbitMQ:StockResponseQueue"] ?? "stock_responses";

            _channel.QueueDeclare(queue: _stockCommandQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(queue: _stockResponseQueue, durable: true, exclusive: false, autoDelete: false);
        }

        public void PublishStockCommand(StockCommand command)
        {
            var message = JsonSerializer.Serialize(command);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: _stockCommandQueue,
                basicProperties: properties,
                body: body
            );
        }

        public void StartListening(Action<StockQuoteResponse> onMessageReceived)
        {
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var response = JsonSerializer.Deserialize<StockQuoteResponse>(message);

                    if (response != null)
                    {
                        onMessageReceived(response);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _stockResponseQueue, autoAck: false, consumer: _consumer);
        }

        public void StopListening()
        {
            _consumer = null;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
