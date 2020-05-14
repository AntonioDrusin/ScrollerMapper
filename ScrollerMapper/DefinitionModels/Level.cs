using System.Collections.Generic;

namespace ScrollerMapper.DefinitionModels
{
    internal class LevelDefinition
    {
        public Dictionary<string, TiledTileDefinition> Tiles { get; set; }
        public Dictionary<string, ImageDefinition> Images { get; set; }
        public Dictionary<string, BobDefinition> Bobs { get; set; }
    }

    internal class TiledTileDefinition
    {
        public string TiledFile { get; set; }
        public int PlaneCount { get; set; }
    }

    internal class ImageDefinition
    {
        public string ImageFile { get; set; }
        public int PlaneCount { get; set; }
    }

    internal class BobDefinition
    {
    }
}