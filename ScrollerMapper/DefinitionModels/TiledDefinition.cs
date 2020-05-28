using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

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
            var definition = tileFileName.FromInputFolder().ReadXmlFile<TiledTileSet>();
            
            return new TileSetDefinition
            {
                ImageFileName = definition.Image.SourceFileName,
                Name = Path.GetFileNameWithoutExtension(tileFileName),
                TileHeight = definition.TileHeight,
                TileWidth = definition.TileWidth,
                ImageHeight = definition.Image.Height,
                ImageWidth = definition.Image.Width,
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

    internal class TiledDefinition
    {
        public TileSetDefinition TileSet { get; private set; }
        public LayerDefinition Layer { get; private set; }
        public string Name { get; private set; }

        public static TiledDefinition Load(string mapFileName)
        {
            var definition = mapFileName.FromInputFolder().ReadXmlFile<TiledMap>();

            return new TiledDefinition
            {
                TileSet = TileSetDefinition.Load(definition.TileSet.Source),
                
                Layer = new LayerDefinition
                {
                    Width = definition.Layer.Width,
                    Height = definition.Layer.Height,
                    Name = definition.Layer.Name,
                    TileIds = ConvertFromCsv(definition.Layer.Data)
                },
                Name = Path.GetFileNameWithoutExtension(mapFileName)
            };
        }

        private static List<int> ConvertFromCsv(string csv)
        {
            var numbers = csv.Replace("\n", "").Split(',');
            return numbers.Select(int.Parse).ToList();
        }

    }

    // Tiled data structure definition

    public class TiledTileSetImage
    {
        [XmlAttribute(AttributeName = "source")]
        public string SourceFileName;
        [XmlAttribute(AttributeName = "width")]
        public int Width;
        [XmlAttribute(AttributeName = "height")]
        public int Height;
    }

    [XmlRoot(ElementName = "tileset")]
    public class TiledTileSet
    {
        [XmlElement(ElementName = "image")]
        public TiledTileSetImage Image;
        [XmlAttribute(AttributeName = "tileheight")]
        public int TileHeight;
        [XmlAttribute(AttributeName = "tilewidth")]
        public int TileWidth;
        [XmlAttribute(AttributeName = "tilecount")]
        public int TileCount;
        [XmlAttribute(AttributeName = "columns")]
        public int Columns;
    }

    public class TiledLayer
    {
        [XmlAttribute(AttributeName = "width")]
        public int Width;

        [XmlAttribute(AttributeName = "height")]
        public int Height;

        [XmlAttribute(AttributeName = "name")] public string Name;
        [XmlElement(ElementName = "data")] public string Data;
    }


    public class TiledMapTileSet
    {
        [XmlAttribute(AttributeName = "source")]
        public string Source;
    }

    [XmlRoot(ElementName = "map")]
    public class TiledMap
    {
        [XmlElement(ElementName = "tileset")] public TiledMapTileSet TileSet;
        [XmlElement(ElementName = "layer")] public TiledLayer Layer;
    }
}