using System;


namespace ScrollerMapper.StreamExtensions
{
    public static class Endian
    {
        public static byte[] ConvertWord(ushort word)
        {
            var array = BitConverter.GetBytes(word);
            if (BitConverter.IsLittleEndian) array = new[] {array[1], array[0]};
            return array;
        }

        public static byte[] ConvertLong(uint data)
        {
            var array = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) array = new[] {array[3], array[2], array[1], array[0]};
            return array;
        }
    }
}