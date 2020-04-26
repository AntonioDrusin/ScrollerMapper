using System.Collections.Generic;
using System.Linq;

namespace ScrollerMapper
{
    internal class TileSetDefinition
    {
        public string ImageFileName;

        public static TileSetDefinition Load(string tileFileName)
        {
            var definition = tileFileName.ReadJsonFile<TiledTileSet>();
            return new TileSetDefinition
            {
                ImageFileName = definition.Image,
            };
        }
    }

    internal class LevelDefinition
    {
        public List<TileSetDefinition> TileSets = new List<TileSetDefinition>();

        public static LevelDefinition Load(string mapFileName)
        {
            var definition = mapFileName.ReadJsonFile<TiledMap>();

            return new LevelDefinition
            {
                TileSets = definition.TileSets.Select(set => TileSetDefinition.Load(set.Image)).ToList(),
            };
        }
    }

    // Tiled data structure definition

    internal class TiledTileSet
    {
        public string Image;
    }

    internal class TiledMapTileSet
    {
        public string Source;
    }

    internal class TiledMap
    {
        public List<TiledTileSet> TileSets;
    }


}