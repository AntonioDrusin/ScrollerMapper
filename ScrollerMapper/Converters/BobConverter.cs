using System.Drawing.Imaging;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Converters
{
    internal class BobConverter
    {
        private readonly BinaryBobRenderer _bobRenderer;

        public BobConverter(BinaryBobRenderer bobRenderer)
        {
            _bobRenderer = bobRenderer;
        }

        public void ConvertAll(string name, BobDefinition definition, int planes, ColorPalette palette)
        {
            var image = definition.ImageFile.FromInputFolder().LoadIndexedBitmap(palette);
            _bobRenderer.Render(name, image, definition.Width, planes);
        }
    }
}
