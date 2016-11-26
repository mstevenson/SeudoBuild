namespace SeudoBuild
{
    public class VersionNumber
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        // e.g. f1, p3, rc2
        public string Build { get; set; } = "";

        public bool IsValid
        {
            get
            {
                return !(Major == 0 && Minor == 0 && Patch == 0);
            }
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{Build}".Replace(' ', '_');
        }
    }
}
