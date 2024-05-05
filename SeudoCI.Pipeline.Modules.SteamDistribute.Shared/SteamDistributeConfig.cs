namespace SeudoCI.Pipeline.Modules.SteamDistribute;

/// <inheritdoc />
/// <summary>
/// Configuration values for a distribute pipeline step that uploads
/// a build product to Steam.
/// </summary>
public class SteamDistributeConfig : DistributeStepConfig
{
    public override string Name => "Steam Upload";
    public string PublishToBranch { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}