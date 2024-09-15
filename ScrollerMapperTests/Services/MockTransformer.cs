using System.Drawing;
using ScrollerMapper.Transformers;

namespace ScrollerMapperTests.Services
{
    internal class MockTransformer : IBitmapTransformer 
    {
        private int _byteWidth;
        private int _height;
        private byte[] _array;

        public void SetArray(int byteWidth, int height, byte[] array)
        {
            _byteWidth = byteWidth;
            _height = height;
            _array = array;
        }

        public void SetBitmap(Bitmap bitmap)
        {
        }

        public int GetByteWidth()
        {
            return _byteWidth;
        }

        public int GetHeight()
        {
            return _height;
        }

        public byte[] GetBitplanes(int planeCount)
        {
            return _array;
        }

        public byte[] GetInterleaved(int planeCount)
        {
            return _array;
        }

        public void FlipColors(byte color1, byte color2)
        {
            throw new System.NotImplementedException();
        }
    }
}
