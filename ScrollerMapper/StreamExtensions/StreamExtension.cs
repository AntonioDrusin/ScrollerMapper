using System;
using System.IO;


namespace ScrollerMapper.StreamExtensions
{
    public static class StreamExtension
    {
        public static BinaryWriter GetBinaryWriter(this string fileName)
        {
            return new BinaryWriter(File.Open(fileName, FileMode.Create));
        }

        public static void WriteWord(this BinaryWriter writer,  ushort word)
        {
            var array = BitConverter.GetBytes(word);
            if (BitConverter.IsLittleEndian)
            {
                array = new[] {array[1], array[0]};
            }

            writer.Write(array);
        }
    }
}
