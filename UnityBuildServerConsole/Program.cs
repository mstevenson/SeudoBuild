using System.Collections.Generic;
using UnityBuildServer;

namespace UnityBuildServerConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var projectConfig = new ProjectConfig
            {
                Id = "Unity Test Project",
                BuildTargets = new List<BuildTargetConfig> {
                    new BuildTargetConfig {
                        Name = "Windows",
                        VCSConfiguration = new GitVCSConfig {
                            RepositoryURL = "",
                            RepositoryBranchName = "",
                            User = "",
                            Password = ""
                        },
                        BuildSteps = new List<BuildStepConfig> {
                            new UnityBuildConfig {
                                TargetPlatform = UnityBuildConfig.Platform.Windows,
                                UnityVersionNumber = new VersionNumber { Major = 5, Minor = 4, Patch = 1 },
                                ExecutableName = "UnityTestBuild",
                                ExecuteMethod = "Builder.BuildForWindows",
                            },
                            new ShellBuildStepConfig {
                                Text = "ls -a"
                            }
                        },
                        ArchiveSteps = new List<ArchiveStepConfig> {
                            new ZipArchiveConfig { Id = "ZipFile", Filename = "UnityTestProject_%platform%_%version%.zip" }
                        },
                        DistributeSteps = new List<DistributeConfig> {
                            new FTPDistributeConfig { ArchiveFileName = "ZipFile", URL = "ftp://abcd.xyz" }
                        },
                        NotifySteps = new List<NotifyConfig> {
                            new EmailNotifyConfig { Id = "Standard Email" }
                        }
                    }
                }
            };

            var builder = new Builder(new BuilderConfig { ProjectsPath = "/Users/mike/Desktop/UnityBuildServerTest/Projects/" });
            builder.ExecuteBuild(projectConfig, "Windows");

            //var pipeline = new ProjectPipeline.Create(projectConfig, "/Users/mike/Desktop/UnityBuildServerTestProject");
        }
    }
}
