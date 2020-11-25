using System.Collections.Generic;

namespace ScrollerMapper.DefinitionModels
{
    internal class GameDefinition
    {
        public LoadingScreenDefinition LoadingScreen { get; set; }
        public MenuDefinition Menu { get; set; }
        public Dictionary<string, LevelLinkDefinition> Levels { get; set; }

        public void Validate()
        {
        }
    }
    
    internal class LevelLinkDefinition
    {
        public string FileName { get; set; }
    }

    internal class MenuDefinition
    {
        public ImageDefinition Background { get; set; }
        public MusicDefinition Music { get; set; }
    }

    internal class LoadingScreenDefinition
    {
        public ImageDefinition Image { get; set; }
    }
}