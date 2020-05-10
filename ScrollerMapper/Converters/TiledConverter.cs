using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ScrollerMapper.LayerInfoRenderers;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.TileRenderers;

namespace ScrollerMapper
{
    internal class TiledConverter : IConverter
    {
        private readonly TileOptions _options;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly ILayerInfoRenderer _layerRenderer;
        private readonly ITileRenderer _tileRenderer;

        public TiledConverter(TileOptions options, IPaletteRenderer paletteRenderer, ILayerInfoRenderer layerRenderer, ITileRenderer tileRenderer)
        {
            _options = options;
            _paletteRenderer = paletteRenderer;
            _layerRenderer = layerRenderer;
            _tileRenderer = tileRenderer;
        }

        public void ConvertAll()
        {
            var definition = LevelDefinition.Load(_options.TileFileName);
            int tileWidth = definition.TileSets.First().TileWidth;
            int tileHeight = definition.TileSets.First().TileHeight;

            foreach (var tileSet in definition.TileSets)
            {
                if (tileSet.TileHeight != tileHeight)
                {
                    throw new ConversionException("All tiles sets must have tiles of the same height.");
                }
                if (tileSet.TileWidth!= tileWidth)
                {
                    throw new ConversionException("All tiles sets must have tiles of the same width.");
                }

                var image = LoadBitmap(tileSet);
                _paletteRenderer.Render(tileSet.Name, image.Palette, _options.PlaneCount.PowerOfTwo());
                _tileRenderer.Render(tileSet.Name, image, tileWidth, tileHeight );
            }

            foreach (var layer in definition.Layers)
            {
                _layerRenderer.Render(layer,_options.PlaneCount, tileWidth, tileHeight);
            }
        }

        private static readonly List<PixelFormat> supportedFormats = new List<PixelFormat>
            {PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed, PixelFormat.Format8bppIndexed};

        private Bitmap LoadBitmap(TileSetDefinition tileSet)
        {
            var bitmap = new Bitmap(tileSet.ImageFileName.FromFolderOf(_options.TileFileName));
            if (!supportedFormats.Contains(bitmap.PixelFormat))
            {
                throw new InvalidOperationException("Only indexed formats are supported");
            }

            return bitmap;
        }
    }
}