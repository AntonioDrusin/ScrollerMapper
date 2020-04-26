using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScrollerMapper
{
    internal class TileSetDefinition
    {
        public string ImageFileName { get; private set; }
        public string Name { get; private set; }
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }

        public static TileSetDefinition Load(string tileFileName)
        {
            var definition = tileFileName.ReadJsonFile<TiledTileSet>();
            return new TileSetDefinition
            {
                ImageFileName = definition.Image,
                Name = Path.GetFileNameWithoutExtension(tileFileName),
                TileHeight = definition.TileHeight,
                TileWidth = definition.TileWidth,
                ImageHeight = definition.ImageHeight,
                ImageWidth = definition.ImageWidth,
            };
        }
    }

    internal class LayerDefinition
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<int> TileIds { get; set; }
    }

    internal class LevelDefinition
    {
        public List<TileSetDefinition> TileSets { get; private set; } = new List<TileSetDefinition>();
        public List<LayerDefinition> Layers { get; private set; }
        public string Name { get; private set; }

        public static LevelDefinition Load(string mapFileName)
        {
            var definition = mapFileName.ReadJsonFile<TiledMap>();

            return new LevelDefinition
            {
                TileSets = definition.TileSets.Select(
                    set => TileSetDefinition.Load(FixFileName(set.Source, mapFileName))
                ).ToList(),
                Layers = definition.Layers.Select(l => new LayerDefinition
                {
                    Width = l.Width,
                    Height = l.Height,
                    Name = l.Name,
                    TileIds = l.Data
                }).ToList(),
                Name = Path.GetFileNameWithoutExtension(mapFileName)
            };
        }

        private static string FixFileName(string fileName, string mapFileName)
        {
            return Path.ChangeExtension(fileName, ".json").FromFolderOf(mapFileName);
        }
    }

    // Tiled data structure definition

    internal class TiledTileSet
    {
        public string Image;
        public int TileHeight;
        public int TileWidth;
        public int ImageWidth;
        public int ImageHeight;
    }

    internal class TiledLayer
    {
        public int Width;
        public int Height;
        public string Name;
        public List<int> Data;
    }


    internal class TiledMapTileSet
    {
        public string Source;
    }

    internal class TiledMap
    {
        public List<TiledMapTileSet> TileSets;
        public List<TiledLayer> Layers;
    }
}