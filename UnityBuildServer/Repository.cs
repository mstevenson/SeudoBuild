using System;
namespace UnityBuildServer
{
    public class Repository
    {
        public string URL { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Branch { get; set; }
    }
}
