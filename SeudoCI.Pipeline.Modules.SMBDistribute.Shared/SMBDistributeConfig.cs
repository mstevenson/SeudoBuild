namespace SeudoCI.Pipeline.Modules.SMBDistribute;

/// <summary>
/// Configuration values for a distribute pipeline step that transfers
/// a build product via SMB.
/// </summary>
public class SMBDistributeConfig : DistributeStepConfig
{
    public override string Name => "SMB Transfer";
    public string Host { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}