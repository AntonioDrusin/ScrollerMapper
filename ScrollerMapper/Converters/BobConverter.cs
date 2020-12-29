using System.Drawing.Imaging;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Converters
{
    internal enum BobMode {
        NoColorFlip,
        ColorFlip
    }

    internal class BobConverter
    {
        private readonly BinaryBobRenderer _bobRenderer;

        public BobConverter(BinaryBobRenderer bobRenderer)
        {
            _bobRenderer = bobRenderer;
        }

        public void ConvertBob(string name, BobDefinition definition, int planes, ColorPalette palette, BobMode colorFlip, Destination destination)
        {
            var image = definition.ImageFile.FromInputFolder().LoadIndexedBitmap(palette);
            _bobRenderer.Render(name, image, definition, planes, colorFlip, destination);
        }
    }
}
