namespace SeudoBuild.Pipeline.Modules.ZipArchive
{
    /// <inheritdoc />
    /// <summary>
    /// Configuration values for an archive pipeline step that generates a zip file.
    /// </summary>
    public class ZipArchiveConfig : ArchiveStepConfig
    {
        public override string Name { get; } = "Zip File";
        public string Filename { get; set; }
    }
}
