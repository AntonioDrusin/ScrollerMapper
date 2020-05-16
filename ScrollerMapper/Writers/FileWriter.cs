﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper
{
    internal class FileWriter : IWriter, IDisposable
    {
        private readonly Options _options;
        private readonly Lazy<IndentedTextWriter> _chipWriter;
        private readonly Lazy<IndentedTextWriter> _constantWriter;
        private BinaryWriter _currentWriter;

        private IndentedTextWriter ChipWriter => _chipWriter.Value;
        private IndentedTextWriter NormalWriter => _constantWriter.Value;

        public FileWriter(Options options)
        {
            _options = options;

            _chipWriter = CreateCodeFile("_chip");
            _constantWriter = CreateCodeFile("");
        }

        private Lazy<IndentedTextWriter> CreateCodeFile(string postFix)
        {
            var fileName = GetFileNameFor(ObjectType.Assembly,
                Path.GetFileNameWithoutExtension(_options.InputFile) + postFix);
            return new Lazy<IndentedTextWriter>(() =>
                new IndentedTextWriter(new StreamWriter(File.Create(fileName)), "\t"));
        }

        public void StartObject(ObjectType type, string name)
        {
            var fileName = GetFileNameFor(type, name);
            ChipWriter.WriteLine($"{name}{GetLabelPostfix(type)}:");
            ChipWriter.Indent++;
            ChipWriter.WriteLine($"incbin {Path.GetFileName(fileName)}");

            _currentWriter = new BinaryWriter(File.Create(fileName));
        }

        public void EndObject()
        {
            ChipWriter.WriteLine("even");
            ChipWriter.Indent--;
            ChipWriter.WriteLine();
            _currentWriter?.Dispose();
            _currentWriter = null;
            ChipWriter.Flush();
            NormalWriter.Flush();
        }

        public void WriteByte(byte data)
        {
            _currentWriter.Write(data);
        }

        public void WriteWord(ushort data)
        {
            _currentWriter.Write(Endian.ConvertWord(data));
        }

        public void WriteLong(uint data)
        {
            _currentWriter.Write(Endian.ConvertLong(data));
        }

        public void WriteBlob(byte[] data)
        {
            _currentWriter.Write(data);
        }

        public void WriteCode(Code codeType, string code)
        {
            switch (codeType)
            {
                case Code.Chip:
                    ChipWriter.WriteLine(code);
                    break;
                case Code.Normal:
                    NormalWriter.WriteLine(code);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codeType), codeType, null);
            }
        }

        public void Dispose()
        {
            if (_chipWriter.IsValueCreated)
            {
                _chipWriter.Value.Close();
                _chipWriter.Value.Dispose();
            }

            if (_constantWriter.IsValueCreated)
            {
                _constantWriter.Value.Close();
                _constantWriter.Value.Dispose();
            }


            _currentWriter?.Dispose();
        }

        private string GetFileNameFor(ObjectType type, string name)
        {
            string extension;
            switch (type)
            {
                case ObjectType.Assembly:
                    extension = "s";
                    break;
                case ObjectType.Palette:
                    extension = "PAL";
                    break;
                case ObjectType.TileInfo:
                    extension = "INFO";
                    break;
                case ObjectType.Bitmap:
                    extension = "BMP";
                    break;
                case ObjectType.Layer:
                    extension = "LAYER";
                    break;
                case ObjectType.Tile:
                    extension = "TILE";
                    break;
                case ObjectType.Bob:
                    extension = "BOB";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (name == null) name = Path.GetFileNameWithoutExtension(_options.InputFile);
            return Path.Combine(_options.OutputFolder, $"{name}.{extension}");
        }

        private string GetLabelPostfix(ObjectType type)
        {
            return type.ToString();
        }
    }
}