using System.Drawing.Imaging;
using System.IO;

namespace ScrollerMapper.PaletteRenderers
{
    public interface IPaletteRenderer
    {
        void Render(BinaryWriter writer, ColorPalette palette, int maxValues);
    }
}