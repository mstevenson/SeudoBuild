using System;

namespace SeudoCI.Pipeline
{
    /// <summary>
    /// The exception that is thrown when a pipeline module DLL can not be loaded.
    /// </summary>
    public class ModuleLoadException : Exception
    {
        public ModuleLoadException() {}

        public ModuleLoadException(string message) : base(message) {}

        public ModuleLoadException(string message, Exception inner) : base(message, inner) {}
    }
}
