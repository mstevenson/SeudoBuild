namespace SeudoCI.Pipeline.Modules.SteamDistribute;

/// <inheritdoc />
/// <summary>
/// Configuration values for a distribute pipeline step that uploads
/// a build product to Steam.
/// </summary>
public class SteamDistributeConfig : DistributeStepConfig
{
    public override string Name => "Steam Upload";
    public string PublishToBranch { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}