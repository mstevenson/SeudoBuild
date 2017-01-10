using System;

namespace SeudoBuild.Pipeline
{
    public class InvalidProjectConfigException : Exception
    {
        public InvalidProjectConfigException() { }

        public InvalidProjectConfigException(string message) : base(message) { }

        public InvalidProjectConfigException(string message, Exception inner) : base(message, inner) { }
    }
}
