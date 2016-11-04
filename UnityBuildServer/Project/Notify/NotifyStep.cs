using System;
namespace UnityBuildServer
{
    public abstract class NotifyStep
    {
        public abstract string TypeName { get; }
        public abstract void Notify();
    }
}
