using System;

namespace SeudoBuild
{
    public class SerializedTypeMap
    {
        public readonly string tag;
        public readonly Type type;

        public SerializedTypeMap(string tag, Type type)
        {
            this.tag = tag;
            this.type = type;
        }
    }
}
