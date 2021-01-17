using System;
using System.Collections.Generic;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.Processors
{
    internal class PathsProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;

        public PathsProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
            _items = items;
        }

        public void Process(LevelDefinition definition)
        {
            WritePaths(definition.Paths);

            if (definition.Player?.Death?.Path != null)
            {
                _writer.StartObject(ObjectType.Data, "PlayerDeathPath");
                WritePathData(definition.Player.Death.Path);
                _writer.EndObject();
            }
            
        }

        public IEnumerable<string> RequiredTypes()
        {
            return null;
        }

        private void WritePaths(Dictionary<string, PathDefinition> definitionPaths)
        {
            WritePathComments();

            _writer.StartObject(ObjectType.Fast, "Paths");
            var initialOffset = _writer.GetCurrentOffset(ObjectType.Fast);

            foreach (var path in definitionPaths)
            {
                var currentOffset = _writer.GetCurrentOffset(ObjectType.Fast);
                _items.Add(ItemTypes.Path, path.Key, currentOffset);

                WritePathData(path.Value);
            }

            Console.WriteLine($"PATHS SIZE: {_writer.GetCurrentOffset(ObjectType.Fast)-initialOffset}");
            _writer.EndObject();
        }

        private void WritePathComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for a path
** The structure is repeated until the FrameCount is 0. That is the end of the path. Enemy will disappear.
** Each path is formed by a number of these structure until framecount is 0.

    structure       PathStructure, 0
    byte            PathInstruction_b
    byte            PathFrameCount_b   
    word            PathVX_w
    word            PathVY_w
    label           PATH_STRUCT_SIZE

;Path instructions
; 0 - Delta
; 1 - End
; 2 - Jump (PathVX_w is the offset of the jump)

");
        }

        public const int PathStructSize = 6;

        private void WritePathData(PathDefinition path)
        {
            var firstTransformer = new SmoothInputPathTransformer();
            var secondTransformer = new OutputPathCoalesceTransformer();
            var finalPath = firstTransformer.TransformPath(path.Steps);
            finalPath = secondTransformer.ProcessPath(finalPath);

            foreach (var step in finalPath)
            {
                _writer.WriteByte((byte)step.Instruction);
                _writer.WriteByte(step.FrameCount);
                _writer.WriteWord((ushort)step.VelocityX);
                _writer.WriteWord((ushort)step.VelocityY);
            }
        }
    }
}
