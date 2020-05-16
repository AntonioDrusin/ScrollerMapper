using System.Drawing;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal interface IBobRenderer
    {
        void Render(string name, Bitmap bitmap, int bobWidth, int planeCount);
    }

    internal class BinaryBobRenderer : IBobRenderer
    {
        private readonly IBitmapTransformer _transformer;
        private readonly IWriter _writer;

        public BinaryBobRenderer(IWriter writer, IBitmapTransformer transformer)
        {
            _writer = writer;
            _transformer = transformer;
        }

        public void Render(string name, Bitmap bitmap, int bobWidth, int planeCount)
        {
            if (bobWidth % 8 != 0)
            {
                throw new ConversionException("Bob width must be a multiple of 8.");
            }

            _transformer.SetBitmap(bitmap);

            var planes = _transformer.GetBitplanes(planeCount);
            var planesWithCookies = MakeCookies(planes, _transformer.GetByteWidth(), _transformer.GetHeight(),
                planeCount, bobWidth / 8);

            _writer.StartObject(ObjectType.Bob, name);
            _writer.WriteBlob(planesWithCookies);
            _writer.EndObject();
        }


        private byte[] MakeCookies(byte[] planes, int byteWidth, int height, int planeCount, int tileByteWidth)
        {
            var numTiles = byteWidth / tileByteWidth;
            var tileWordWidth = (tileByteWidth + 1) / 2;

            var result = new byte[numTiles * tileWordWidth * 2 * height * (planeCount*2)];

            for (int t = 0; t < numTiles; t++)
            {
                for (int xb = 0; xb < tileByteWidth; xb++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte mask = 0;

                        for (int bpl = 0; bpl < planeCount; bpl++)
                        {
                            var value = planes[
                                t * tileByteWidth + xb + y * byteWidth + bpl * byteWidth * height
                            ];
                            mask |= value;
                            result[
                                t * (tileWordWidth * 2) * height * planeCount * 2 + xb + y * (tileWordWidth * 2) * planeCount + bpl * (tileWordWidth * 2)
                            ] = value;
                        }

                        for (var mPlane = 0; mPlane < planeCount; mPlane++)
                        {
                            result[
                                tileWordWidth*2*height*planeCount+
                                t * (tileWordWidth * 2) * height * planeCount * 2 + xb + y * (tileWordWidth * 2) * planeCount + (mPlane) * (tileWordWidth * 2)
                            ] = mask;
                        }
                    }
                }
            }

            return result;
        }
    }
}