namespace UnityBuild
{
    public class VersionNumber
    {
        public int Major { get; set; } = 1;
        public int Minor { get; set; }
        public int Patch { get; set; }
        // e.g. f1, p3, rc2
        public string Build { get; set; } = "";

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{Build}";
        }
    }
}
