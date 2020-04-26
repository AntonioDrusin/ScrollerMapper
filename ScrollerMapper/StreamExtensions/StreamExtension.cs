using System.IO;
using System.Text;

namespace ScrollerMapper.StreamExtensions
{
    public static class StreamExtension
    {
        public static void WriteLine(this BinaryWriter writer, string line)
        {
            writer.Write(Encoding.ASCII.GetBytes(line));
            writer.Write(0x0a);
        }
    }
}
