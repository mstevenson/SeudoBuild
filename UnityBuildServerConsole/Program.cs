using System.Collections.Generic;
using UnityBuildServer;
using UnityBuildServer.VersionControl;

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
                        Id = "Windows",
                        VCSConfiguration = new GitConfiguration {
                            RepositoryURL = "",
                            RepositoryBranchName = "",
                            User = "",
                            Password = ""
                        },
                        BuildSteps = new List<BuildStepConfig> {
                            new UnityBuildStep {
                                TargetPlatform = UnityBuildStep.Platform.Windows,
                                UnityVersionNumber = new VersionNumber { Major = 5, Minor = 4, Patch = 1 },
                                ExecutableName = "UnityTestBuild",
                                ExecuteMethod = "Builder.BuildForWindows",
                            },
                            new ShellBuildStep {
                                Text = "ls -a"
                            }
                        },
                        Archives = new List<ArchiveStepConfig> {
                            new ZipArchiveStepConfig { Id = "ZipFile", Filename = "UnityTestProject_%platform%_%version%.zip" }
                        },
                        Distributions = new List<DistributionConfig> {
                            new FTPDistributionConfig { ArchiveFileName = "ZipFile", URL = "ftp://abcd.xyz" }
                        },
                        Notifications = new List<NotificationConfig> {
                            new EmailNotificationConfig { Id = "Standard Email" }
                        }
                    }
                }
            };

            Project.Create(projectConfig, "/Users/mike/Desktop/UnityBuildServerTestProject");
        }
    }
}
