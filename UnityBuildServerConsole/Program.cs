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
                        VCSConfiguration = new GitVCSConfiguration {
                            RepositoryURL = "",
                            RepositoryBranchName = "",
                            User = "",
                            Password = ""
                        },
                        BuildSteps = new List<BuildStepConfig> {
                            new UnityBuildStepConfig {
                                TargetPlatform = UnityBuildStepConfig.Platform.Windows,
                                UnityVersionNumber = new VersionNumber { Major = 5, Minor = 4, Patch = 1 },
                                ExecutableName = "UnityTestBuild",
                                ExecuteMethod = "Builder.BuildForWindows",
                            },
                            new ShellBuildStepConfig {
                                Text = "ls -a"
                            }
                        },
                        ArchiveSteps = new List<ArchiveStepConfig> {
                            new ZipArchiveStepConfig { Id = "ZipFile", Filename = "UnityTestProject_%platform%_%version%.zip" }
                        },
                        DistributionSteps = new List<DistributionConfig> {
                            new FTPDistributionConfig { ArchiveFileName = "ZipFile", URL = "ftp://abcd.xyz" }
                        },
                        NotificationSteps = new List<NotificationConfig> {
                            new EmailNotificationConfig { Id = "Standard Email" }
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
