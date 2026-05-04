using System.Text;
using System.Text.Json;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HireOps.Infrastructure.Services;

public class RabbitMqService : IRabbitMqService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _defaultExchange;

    public RabbitMqService(
        IConfiguration config,
        ILogger<RabbitMqService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _defaultExchange = config["RabbitMQ:ExchangeName"] ?? "sim.pipeline";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default)
        where T : class
    {
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _jsonOptions));

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: new ReadOnlyMemory<byte>(body),
            cancellationToken: ct);

        _logger.LogDebug("Published to {Exchange}/{RoutingKey}: {Message}",
            exchange, routingKey, typeof(T).Name);
    }
    
    public async Task SetQosAsync(int prefetchCount, CancellationToken ct = default)
    {
        // prefetchCount = сколько сообщений консьюмер берёт "в работу" одновременно
        // 0 = без лимита (не рекомендуется), 1 = строго по одному, 10-50 = оптимально для CPU-задач
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: (ushort)prefetchCount, global: false, ct);
        _logger.LogDebug("Set QoS: prefetchCount={Prefetch}", prefetchCount);
    }
    
    public async Task<ConsumerSubscription> ConsumeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct = default)
        where T : class
    {
        await _channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await _channel.QueueBindAsync(queue: queue, exchange: _defaultExchange, routingKey: queue,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(ea.Body.Span), _jsonOptions);
                if (message != null)
                {
                    await handler(message, ct);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        
        var result =
            await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer, cancellationToken: ct);

        _logger.LogInformation("Started consuming from queue: {Queue} (tag: {Tag})", queue, result);
        return new ConsumerSubscription(result, queue);
    }

    public async Task CancelConsumerAsync(string consumerTag, CancellationToken ct = default)
    {
        await _channel.BasicCancelAsync(consumerTag, cancellationToken: ct);
        _logger.LogDebug("Cancelled consumer with tag: {Tag}", consumerTag);
    }
    
    public async Task<ConsumerSubscription> ConsumeAndMediateAsync<TMessage, TCommand>(
        string queue,
        Func<TMessage, TCommand> mapToCommand,
        CancellationToken ct = default)
        where TMessage : class
        where TCommand : IRequest
    {
        await _channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await _channel.QueueBindAsync(queue: queue, exchange: _defaultExchange, routingKey: queue,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(Encoding.UTF8.GetString(ea.Body.Span), _jsonOptions);
                if (message != null)
                {
                    var command = mapToCommand(message);
                    await mediator.Send(command, ct);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        var result =
            await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer, cancellationToken: ct);
        _logger.LogInformation("Started consuming and mediating from queue: {Queue} (tag: {Tag})", queue,
            result);
        return new ConsumerSubscription(result, queue);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}