using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Unity
{
    public class SeudoBuildEditor : EditorWindow
    {
        ProjectConfig config = new ProjectConfig();

        [MenuItem("Window/SeudoBuild")]
        static void Init()
        {
            var window = GetWindow<SeudoBuildEditor>();
            window.LoadOrCreateCache();
        }

        string Filepath
        {
            get
            {
                return Path.Combine(Application.dataPath, "..", "Library", "SeudoBuildCache.json");
            }
        }

        void LoadOrCreateCache()
        {
            string filepath = Filepath;
            if (File.Exists(filepath))
            {
                config = LoadConfig(filepath);
            }
            else
            {
                config.ProjectName = PlayerSettings.productName;
                Save(config, filepath);
            }
        }

        ProjectConfig LoadConfig(string filepath)
        {
            string json = File.ReadAllText(filepath);
            var result = JsonConvert.DeserializeObject<ProjectConfig>(json);
            return result;
        }

        void Save(ProjectConfig configuration, string filepath)
        {
            string json = JsonConvert.SerializeObject(configuration);
            File.WriteAllText(json, filepath);
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            config.ProjectName = EditorGUILayout.TextField("Project Name", config.ProjectName);

            EditorGUILayout.LabelField("Targets");
            foreach (var target in config.BuildTargets)
            {
                target.TargetName = EditorGUILayout.TextField("Target Name", target.TargetName);
                // TODO version number

                EditorGUILayout.LabelField("Source Steps");
                foreach (var step in target.SourceSteps)
                {
                }

                EditorGUILayout.LabelField("Build Steps");
                foreach (var step in target.BuildSteps)
                {
                }

                EditorGUILayout.LabelField("Archive Steps");
                foreach (var step in target.ArchiveSteps)
                {
                }

                EditorGUILayout.LabelField("Distribute Steps");
                foreach (var step in target.DistributeSteps)
                {
                }

                EditorGUILayout.LabelField("Notify Steps");
                foreach (var step in target.NotifySteps)
                {
                }
            }

            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                Save(config, Filepath);
            }
        }
    }
}
