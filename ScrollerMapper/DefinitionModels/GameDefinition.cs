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
        public Dictionary<string, FontDefinition> Fonts;
    }

    internal class FontDefinition
    {
        public string Characters = "0123456789";
        public int VerticalDistance;

        public int X = 0;
        public int Y = 0;
        public int Height = 8;
        public int Width = 8;
        public string ImageFile;
        public string PaletteFile;
        public int PlaneCount = 1;
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