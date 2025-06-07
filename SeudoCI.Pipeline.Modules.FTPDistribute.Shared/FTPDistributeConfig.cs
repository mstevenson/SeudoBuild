namespace SeudoCI.Pipeline.Modules.FTPDistribute;

/// <summary>
/// Configuration values for a distribute pipeline step that uploads
/// a build product via FTP.
/// </summary>
public class FTPDistributeConfig : DistributeStepConfig
{
    public override string Name => "FTP Upload";
    public string URL { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string BasePath { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}