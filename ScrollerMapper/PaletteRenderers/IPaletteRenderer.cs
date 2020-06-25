using System.Drawing.Imaging;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.PaletteRenderers
{
    public interface IPaletteRenderer
    {
        void Render(PaletteTransformer palette);
    }
}