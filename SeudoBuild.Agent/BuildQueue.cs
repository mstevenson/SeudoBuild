using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace SeudoBuild.Agent
{
    public class BuildQueue : IDisposable
    {
        CancellationTokenSource cancelTokenSource;
        CancellationToken cancelToken;

        public void Start()
        {
            Console.WriteLine("Starting server");
            cancelTokenSource = new CancellationTokenSource();
            cancelToken = cancelTokenSource.Token;


            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "hello",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine("  Received {0}", message);
                    };
                    channel.BasicConsume(queue: "hello",
                                         noAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }

            //var task = Task.Factory.StartNew(ServerTask, cancelToken, TaskCreationOptions.LongRunning, null);
        }

        void ServerTask()
        {
            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // TODO
                }
            }
            finally
            {
                // TODO cleanup
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server");
            cancelTokenSource.Cancel();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
