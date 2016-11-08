using System;
namespace SeudoBuild.Agent
{
    public class BuildServer : IDisposable
    {
        public void Start()
        {
            Console.WriteLine("Starting server");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
