using System;
using System.Collections.Generic;

namespace ScrollerMapper.HeaderRenderers
{
    internal class HeaderRenderer
    {
        private readonly IWriter _writer;
        private readonly HashSet<Tuple<string, ObjectType>> _doneComments =new HashSet<Tuple<string, ObjectType>>();

        public HeaderRenderer(IWriter writer)
        {
            _writer = writer;
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
            var key = Tuple.Create(name, type); // In case we create multiple level files
            if (_doneComments.Contains(key)) return;
            _doneComments.Add(key);

            _writer.WriteCode(Code.Normal, $"\n\n; Structure for file for {name} in {type} ram");
            _writer.WriteCode(Code.Normal, $"\tstructure   {name}{type}Structure, 0");
            foreach (var element in objectNames)
            {
                _writer.WriteCode(Code.Normal, $"\tlong\t\t{name}{type}{element}Ptr_l");
            }
            _writer.WriteCode(Code.Normal, $"\tlabel       {name.ToUpper()}_{type.ToString().ToUpper()}_STRUCT_SIZE");
        }

        public void WriteHeaderOffsets(string name, ObjectType type, List<string> objectNames)
        {
            _writer.RestartObject(type, $"FileStructure{type}");
            foreach (var header in objectNames)
            {
                _writer.WriteLong((uint) _writer.GetOffset(header));
            }
            _writer.EndObject();
        }

    }
}
