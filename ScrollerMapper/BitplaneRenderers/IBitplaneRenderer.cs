using System.Drawing;
using ScrollerMapper.Writers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal interface IBitplaneRenderer
    {
        void Render(string name, Bitmap bitmap, int planeCount, Destination destination);
    }
}
