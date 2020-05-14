using System.Drawing;

namespace ScrollerMapper.ImageRenderers
{
    internal interface IBitplaneRenderer
    {
        void Render(string name, Bitmap bitmap, int planeCount);
    }
}
