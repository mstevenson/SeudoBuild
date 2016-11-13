﻿using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityParameterizedBuildModule : IBuildModule
    {
        public Type ArchiveStepType { get; } = typeof(UnityParameterizedBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(BuildStepConfig config)
        {
            return config is UnityParameterizedBuildConfig;
        }
    }
}
