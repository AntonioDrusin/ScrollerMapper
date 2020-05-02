using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.ImageRenderers;
using ScrollerMapper.LayerInfoRenderers;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.TileRenderers;

namespace ScrollerMapper
{
    internal class TileSetConverter : ITileSetConverter
    {
        private readonly Options _options;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly ILayerInfoRenderer _layerRenderer;
        private readonly IBitplaneRenderer _bitplaneRenderer;
        private readonly ITileRenderer _tileRenderer;

        public TileSetConverter(Options options, IPaletteRenderer paletteRenderer, ILayerInfoRenderer layerRenderer, IBitplaneRenderer bitplaneRenderer, ITileRenderer tileRenderer)
        {
            _options = options;
            _paletteRenderer = paletteRenderer;
            _layerRenderer = layerRenderer;
            _bitplaneRenderer = bitplaneRenderer;
            _tileRenderer = tileRenderer;
        }

        public void ConvertAll()
        {
            var definition = LevelDefinition.Load(_options.TileFileName);
            foreach (var tileSet in definition.TileSets)
            {
                var image = LoadBitmap(tileSet);
                _paletteRenderer.Render(tileSet.Name, image.Palette, _options.PlaneCount.PowerOfTwo());
                _bitplaneRenderer.Render(tileSet.Name, image);
                _tileRenderer.Render(tileSet.Name, image, tileSet.TileWidth, tileSet.TileHeight );
            }

            foreach (var layer in definition.Layers)
            {
                _layerRenderer.Render(layer);
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