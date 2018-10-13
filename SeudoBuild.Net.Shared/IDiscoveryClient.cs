using System;

namespace SeudoBuild.Net
{
    public interface IDiscoveryClient<out T>
        where T : IDiscoveryBeacon
    {
        event Action<T> ServerFound;
        event Action<T> ServerLost;
        
        bool IsRunning { get; }

        void Start();
        void Stop();
    }
}