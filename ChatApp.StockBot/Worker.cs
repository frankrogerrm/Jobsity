using ChatApp.Shared.Models;
using ChatApp.StockBot.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatApp.StockBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Bot Worker started at: {time}", DateTimeOffset.Now);

            try
            {
                InitializeRabbitMQ();

                var stockCommandQueue = _configuration["RabbitMQ:StockCommandQueue"] ?? "stock_commands";

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var command = JsonSerializer.Deserialize<StockCommand>(message);

                        if (command != null)
                        {
                            _logger.LogInformation($"Processing stock command: {command.StockCode}");

                            using var scope = _serviceProvider.CreateScope();
                            var stockQuoteService = scope.ServiceProvider.GetRequiredService<IStockQuoteService>();
                            var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();

                            var response = await stockQuoteService.GetStockQuoteAsync(command);

                            rabbitMQService.PublishStockResponse(response);

                            _logger.LogInformation($"Stock command processed successfully: {response.Message}");
                        }

                        _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing stock command");
                        _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel?.BasicConsume(queue: stockCommandQueue, autoAck: false, consumer: consumer);

                _logger.LogInformation("Stock Bot is now listening for commands...");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Stock Bot Worker");
                throw;
            }
        }

        private void InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var stockCommandQueue = _configuration["RabbitMQ:StockCommandQueue"] ?? "stock_commands";

            _channel.QueueDeclare(
                queue: stockCommandQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("RabbitMQ connection established");
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
            _logger.LogInformation("Stock Bot Worker disposed");
        }
    }
}
