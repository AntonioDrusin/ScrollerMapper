﻿using System;
using System.IO;
using ScrollerMapper;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapperTests.Services
{
    internal class MockWriter : IWriter
    {
        private MemoryStream _stream;

        public byte[] Data => _stream.ToArray();

        public void StartObject(ObjectType type, string name)
        {
            _stream = new MemoryStream();
        }

        public void CompleteObject()
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

        public void WriteCode(string code)
        {
            throw new NotImplementedException();
        }
    }
}