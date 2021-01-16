using System.Collections.Generic;

namespace ScrollerMapper.DefinitionModels
{
    internal class GameDefinition
    {
        public LoadingScreenDefinition LoadingScreen;
        public MenuDefinition Menu;
        public Dictionary<string, LevelLinkDefinition> Levels;
        public GamePanelDefinition Panel;
        public Dictionary<string, SpriteDefinition> Sprites;
    }

    internal class GamePanelDefinition
    {
        public LivesDefinition Lives;
        public int PlaneCount;
        public string Palette;
    }

    internal class LivesDefinition
    {
        public int X;
        public int Y;
        public int Max;
        public int Start;
        public BobDefinition Bob;
    }

    internal class LevelLinkDefinition
    {
        public string FileName;
    }

    internal class MenuDefinition
    {
        public ImageDefinition Background;
        public MusicDefinition Music;
    }

    internal class LoadingScreenDefinition
    {
        public ImageDefinition Image;
    }
}