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
        private LevelDefinition _definition;

        public PathsProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
            _items = items;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;
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

            Console.WriteLine($"PATHS SIZE: {_writer.GetCurrentOffset(ObjectType.Fast) - initialOffset}");
            _writer.EndObject();
        }

        private void WritePathComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for a path
** The structure is repeated until the FrameCount is 0. That is the end of the path. Enemy will disappear.
** Each path is formed by a number of these structure until framecount is 0.

    structure       PathStructureBase, 0
    byte            PathInstruction_b
    byte            PathFrameCount_b   
    label           PATH_STRUCT_BASE_SIZE

    structure       PathStructure, PATH_STRUCT_BASE_SIZE
    word            PathVX_w
    word            PathVY_w
    label           PATH_STRUCT_SIZE

    structure       PathStructureHoming, PATH_STRUCT_BASE_SIZE
    word            PathMaxVel_w
    word            PathAccel_w

;Path instructions
; 0 - Delta
; 1 - End
; 2 - Jump (PathVX_w is the offset of the jump)
; 3 - Home on target. PathVX_w is max speed PathVY_w is acceleration per frame
;

");
        }

        public const int PathStructSize = 6;

        private void WritePathData(PathDefinition path)
        {
            IEnumerable<OutputPathStepInfo> outputPath;
            switch (path.Mode)
            {
                case PathModeDefinition.Delta:
                    outputPath = ProcessDeltaPath(path.Steps);
                    break;
                case PathModeDefinition.CenterToCircle:     // Now I wish I implemented this as a step.
                    outputPath = ProcessCircularPath(path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var step in outputPath)
            {
                _writer.WriteByte((byte) step.Instruction);
                _writer.WriteByte((byte)step.FrameCount);
                _writer.WriteWord((ushort) step.VelocityX);
                _writer.WriteWord((ushort) step.VelocityY);
            }
        }

        private IEnumerable<OutputPathStepInfo> ProcessCircularPath(PathDefinition path)
        {
            var output = new List<PathStepDefinition>();
            // First, at velocity, go up until at radius distance from the start.
            double toX = path.CenterX;
            double toY = path.CenterY - (path.Diameter / 2);
            var distanceX = toX - path.StartX;
            var distanceY = toY - path.StartY;
            double distance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));
            var initialSteps = distance / path.Speed;

            output.Add(new PathStepDefinition
            {
                F = (int) initialSteps,
                X = ToFixedPoint(distanceX / initialSteps),
                Y = ToFixedPoint(distanceY / initialSteps)
            });

            // Now perform a circle and go back to the beginning
            var angularVelocity = (Math.PI * 2.0) * (path.Speed / (path.Diameter * Math.PI));
            int fX = ToFixedPoint(toX);
            int fY = ToFixedPoint(toY);
            bool first = true;
            for (double t = Math.PI * 2.0; t > 0; t -= angularVelocity)
            {
                double newX = path.CenterX - (path.Diameter / 2.0) * Math.Sin(t);
                double newY = path.CenterY - (path.Diameter / 2.0) * Math.Cos(t);
                var dX = ToFixedPoint(newX - ToDouble(fX));
                var dY = ToFixedPoint(newY - ToDouble(fY));
                output.Add(new PathStepDefinition
                {
                    F = 1,
                    X = dX,
                    Y = dY,
                    Label = first ? "REPEAT" : null,
                });
                fX += dX;
                fY += dY;
                first = false;
            }

            output.Add(new PathStepDefinition
            {
                Label = "REPEAT",
                Instruction = PathInstructionDefinition.Jump,
            });
            return ProcessDeltaPath(output);
        }

        private int ToFixedPoint(double v)
        {
            var multiplier = Math.Pow(2, _definition.FixedPointBits);
            return (int) (v * multiplier);
        }

        private int ToRoundedFixedPoint(double v)
        {
            var multiplier = Math.Pow(2, _definition.FixedPointBits);
            return (int)(Math.Round(v) * multiplier);
        }


        private double ToDouble(int v)
        {
            var multiplier = Math.Pow(2, _definition.FixedPointBits);
            return (double) v / multiplier;
        }


        private IEnumerable<OutputPathStepInfo> ProcessDeltaPath(List<PathStepDefinition> steps)
        {
            var firstTransformer = new SmoothInputPathTransformer();
            var secondTransformer = new OutputPathCoalesceTransformer();
            var finalPath = firstTransformer.TransformPath(steps);
            finalPath = secondTransformer.ProcessPath(finalPath);
            return finalPath;
        }
    }
}