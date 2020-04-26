using System.Drawing.Imaging;

namespace ScrollerMapper.PaletteRenderers
{
    public interface IPaletteRenderer
    {
        void Render(string name, ColorPalette palette, int maxValues);
    }
}