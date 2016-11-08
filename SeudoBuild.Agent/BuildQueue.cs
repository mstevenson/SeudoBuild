using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

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


            using (var server = new ResponseSocket())
            {
                server.Bind("tcp://*:5555");

                while (true)
                {
                    var message = server.ReceiveFrameString();

                    Console.WriteLine("Received {0}", message);

                    // processing the request
                    Thread.Sleep(100);

                    Console.WriteLine("Sending World");
                    server.SendFrame("World");
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
