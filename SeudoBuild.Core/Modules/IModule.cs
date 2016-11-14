﻿using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IModule
    {
        string Name { get; }
        Type StepType { get; }
        Type StepConfigType { get; }
        string StepConfigName { get; }
    }
}
