using System.IO;
using ScrollerMapper.StreamExtensions;
using ScrollerMapper.Writers;

namespace ScrollerMapperTests.Services
{
    internal class MockWriter : IWriter
    {
        private MemoryStream _stream;

        public byte[] Data => _stream.ToArray();

        public void StartDiskFile(string diskFileName)
        {
            throw new System.NotImplementedException();
        }

        public void CompleteDiskFile()
        {
            throw new System.NotImplementedException();
        }

        public void StartObject(ObjectType type, string name)
        {
            _stream = new MemoryStream();
        }

        public void RestartObject(ObjectType type, string name)
        {
            throw new System.NotImplementedException();
        }

        public void EndObject()
        {
        }

        public void WriteByte(byte data)
        {
            _stream.WriteByte(data);
        }

        public void WriteWord(ushort data)
        {
            _stream.Write(Endian.ConvertWord(data), 0, 2);
        }

        public void WriteLong(uint data)
        {
            _stream.Write(Endian.ConvertLong(data), 0, 4);
        }

        public void WriteBlob(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
        }

        public void WriteBlob(byte[] data, int count)
        {
            throw new System.NotImplementedException();
        }

        public void WriteCode(Code codeType, string code)
        {
        }

        public void WriteOffset(ObjectType objectType, uint offset)
        {
            throw new System.NotImplementedException();
        }

        public void WriteRelocations()
        {
            throw new System.NotImplementedException();
        }

        public uint GetCurrentOffset(ObjectType objectType)
        {
            throw new System.NotImplementedException();
        }

        public uint GetOffset(string name)
        {
            throw new System.NotImplementedException();
        }

    }
}