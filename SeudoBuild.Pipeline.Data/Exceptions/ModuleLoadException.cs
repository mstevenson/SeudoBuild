using System;

namespace SeudoBuild.Pipeline
{
    public class ModuleLoadException : Exception
    {
        public ModuleLoadException() {}

        public ModuleLoadException(string message) : base(message) {}

        public ModuleLoadException(string message, Exception inner) : base(message, inner) {}
    }
}
