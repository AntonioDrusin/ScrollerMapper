﻿using System.Collections.Generic;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal interface IProcessor
    {
        void Process(LevelDefinition definition);
        IEnumerable<string> RequiredTypes();
    }
}
