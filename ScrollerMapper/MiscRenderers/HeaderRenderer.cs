using System;
using System.Collections.Generic;
using ScrollerMapper.Writers;

namespace ScrollerMapper.MiscRenderers
{
    internal class HeaderRenderer
    {
        private readonly IWriter _writer;
        private readonly ICodeWriter _codeWriter;

        public HeaderRenderer(IWriter writer, ICodeWriter codeWriter)
        {
            _writer = writer;
            _codeWriter = codeWriter;
        }

        public void WriteHeader(string name, ObjectType type, List<string> objectNames)
        {
            if ( type != ObjectType.Chip && type != ObjectType.Fast) throw new NotSupportedException("Only Chip and Fast types are supported");

            WriteStructureParts(name, type, objectNames);

            var headersSize = objectNames.Count * 4;
            byte[] blank = new byte[headersSize];
            _writer.StartObject(type, $"FileStructure{type}");
            _writer.WriteBlob(blank, headersSize);
            _writer.EndObject();
        }

        private void WriteStructureParts(string name, ObjectType type, List<string> objectNames)
        {
            _codeWriter.WriteStructureDeclarationOfLongs(name, type.ToString(), objectNames);
        }

        public void WriteHeaderOffsets(string name, ObjectType type, List<string> objectNames)
        {
            _writer.RestartObject(type, $"FileStructure{type}");
            foreach (var header in objectNames)
            {
                _writer.WriteOffset(type, _writer.GetOffset(header));
            }
            _writer.EndObject();
        }
    }
}
