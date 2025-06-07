namespace SeudoCI.Pipeline.Modules.PerforceSource.Shared;

public class PerforceConfig : SourceStepConfig
{
    public override string Name => "Perforce";
    public string Server { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
}