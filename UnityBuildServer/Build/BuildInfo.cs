using System;
using System.Collections.Generic;

namespace UnityBuildServer
{
    public class BuildInfo
    {
        public string ProjectName { get; set; }
        public string BuildTargetName { get; set; }
        public DateTime BuildDate { get; set; }
        public string CommitIdentifier { get; set; }
        public VersionNumber AppVersion { get; set; }

        public enum Element
        {
            ProjectName,
            BuildTargetName,
            BuildDate,
            CommitIdentifier,
            AppVersion
        }

        public string GenerateFileName(params Element[] elements)
        {
            if (elements.Length == 0)
            {
                return GenerateFileName(Element.ProjectName, Element.BuildTargetName);
            }

            var parts = new List<string>();

            foreach (var e in elements)
            {
                switch (e)
                {
                    case Element.ProjectName:
                        parts.Add(ProjectName);
                    break;
                    case Element.BuildTargetName:
                        parts.Add(BuildTargetName);
                    break;
                    case Element.BuildDate:
                        parts.Add(BuildDate.ToString("yyyy-dd-M--HH-mm-ss"));
                    break;
                    case Element.CommitIdentifier:
                        parts.Add(CommitIdentifier);
                    break;
                    case Element.AppVersion:
                        parts.Add(AppVersion.ToString());
                    break;
                }
            }

            string output = string.Join("_", parts.ToArray()).Replace(' ', '_');
            return output;
        }
    }
}
