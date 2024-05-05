namespace SeudoCI.Pipeline.Modules.Perforce.Shared;

public class PerforceConfig : SourceStepConfig
{
    public override string Name => "Perforce";
    public string Server { get; set; }
    public string User { get; set; }
    public string Pass { get; set; }
    public string Client { get; set; }
}