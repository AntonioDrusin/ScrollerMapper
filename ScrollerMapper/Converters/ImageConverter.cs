using System.IO;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.ImageRenderers;
using ScrollerMapper.PaletteRenderers;

namespace ScrollerMapper.Converters
{
    internal class ImageConverter
    {
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly IBitplaneRenderer _bitplaneRenderer;

        public ImageConverter(IPaletteRenderer paletteRenderer, IBitplaneRenderer bitplaneRenderer)
        {
            _paletteRenderer = paletteRenderer;
            _bitplaneRenderer = bitplaneRenderer;
        }

        public void ConvertAll(string name, ImageDefinition definition)
        {
            var fileName = definition.ImageFile;
            var image = fileName.FromInputFolder().LoadIndexedBitmap();
            _bitplaneRenderer.Render(name, image, definition.PlaneCount);
            _paletteRenderer.Render(name, image.Palette, definition.PlaneCount.PowerOfTwo());
        }
    }
}
