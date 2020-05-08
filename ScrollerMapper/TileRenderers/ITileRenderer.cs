using System.Drawing;

namespace ScrollerMapper.TileRenderers
{
    internal interface ITileRenderer
    {
        void Render(string name, Bitmap bitmap, int tileWidth, int tileHeight);
    }
}