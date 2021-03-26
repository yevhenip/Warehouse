﻿using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Warehouse.Api.Messaging.Receiver
{
    public abstract class Receiver<T> : BackgroundService
    {
        private readonly string _queue;
        private readonly IConnection _connection;
        private IModel _channel;

        protected Receiver(IConnection connection, string queue)
        {
            _connection = connection;
            _queue = queue;
            InitializeRabbitMqListener();
        }

        /// <summary>
        /// Initializing channel
        /// </summary>
        private void InitializeRabbitMqListener()
        {
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_queue, false, false, true, null);
        }

        /// <summary>
        /// Defines receiving of message 
        /// </summary>
        /// <param name="stoppingToken"></param>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (_, eventArgs) =>
            {
                var content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var item = JsonSerializer.Deserialize<T>(content);

                await HandleMessage(item);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };
            
            _channel.BasicConsume(_queue, false, consumer);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Does needed operation with received data
        /// </summary>
        /// <param name="item"></param>
        protected abstract Task HandleMessage(T item);

        /// <summary>
        /// Closes all connections
        /// </summary>
        ~Receiver()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}