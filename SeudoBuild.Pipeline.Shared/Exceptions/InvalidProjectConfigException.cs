using System;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// The exception that is thrown when a project has not been configured correctly.
    /// This is likely due to a malformed project configuration file.
    /// </summary>
    public class InvalidProjectConfigException : Exception
    {
        public InvalidProjectConfigException() { }

        public InvalidProjectConfigException(string message) : base(message) { }

        public InvalidProjectConfigException(string message, Exception inner) : base(message, inner) { }
    }
}
