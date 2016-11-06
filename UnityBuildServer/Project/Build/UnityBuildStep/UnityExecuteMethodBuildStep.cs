using System.Diagnostics;
using System.Threading.Tasks;
using RunProcessAsTask;
using System.IO;

namespace UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep
    {
        UnityExecuteMethodBuildConfig config;
        Workspace workspace;

        public UnityExecuteMethodBuildStep(UnityExecuteMethodBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Execute Method";
            }
        }

        public override void Execute()
        {
            // TODO
        }

        Task<ProcessResults> Run(UnityInstallation unity, UnityExecuteMethodBuildConfig config, Workspace workspace)
        {
            if (!File.Exists(unity.Path))
            {
                throw new System.Exception("Unity executable does not exist at path " + unity.Path);
            }

            var args = "-quit -batchmode -executeMethod " + config.MethodName;
            var startInfo = new ProcessStartInfo(unity.Path, args);

            var task = ProcessEx.RunAsync(startInfo);
            return task;
        }
    }
}
