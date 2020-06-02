using System.Linq;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.LayerInfoRenderers;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.TileRenderers;

namespace ScrollerMapper.Converters
{
    internal class TiledConverter
    {
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly ILayerInfoRenderer _layerRenderer;
        private readonly ITileRenderer _tileRenderer;

        public TiledConverter(
            IPaletteRenderer paletteRenderer,
            ILayerInfoRenderer layerRenderer,
            ITileRenderer tileRenderer)
        {
            _paletteRenderer = paletteRenderer;
            _layerRenderer = layerRenderer;
            _tileRenderer = tileRenderer;
        }

        public void ConvertAll(string name, TiledTileDefinition definition)
        {
            var tiled = TiledDefinition.Load(definition.TiledFile);
            int tileWidth = tiled.TileSet.TileWidth;
            int tileHeight = tiled.TileSet.TileHeight;

            var tileSet = tiled.TileSet;
            if (tileSet.TileHeight != tileHeight)
            {
                throw new ConversionException("All tiles sets must have tiles of the same height.");
            }

            if (tileSet.TileWidth != tileWidth)
            {
                throw new ConversionException("All tiles sets must have tiles of the same width.");
            }

            var image = tileSet.ImageFileName.FromInputFolder().LoadIndexedBitmap();
            _paletteRenderer.Render(name, image.Palette, definition.PlaneCount.PowerOfTwo());
            _tileRenderer.Render(name, image, tileWidth, tileHeight, definition.PlaneCount);

            var layer = tiled.Layer;

            _layerRenderer.Render(name, layer, definition.PlaneCount, tileWidth, tileHeight);
        }
    }
}