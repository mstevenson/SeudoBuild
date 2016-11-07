using System;
namespace UnityBuild
{
    public abstract class NotifyStep
    {
        public abstract string Type { get; }
        public abstract void Notify();
    }
}
