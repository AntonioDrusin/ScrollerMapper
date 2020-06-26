using System.Linq;
using System.Text;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.Processors
{
    internal class CopperShadeProcessor : IProcessor
    {
        private readonly LevelTransformer _levelTransformer;
        private readonly IWriter _writer;
        private LevelDefinition _definition;

        public CopperShadeProcessor(LevelTransformer levelTransformer, IWriter writer)
        {
            _levelTransformer = levelTransformer;
            _writer = writer;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;
            if (definition.Background?.CopperShade != null)
            {
                _levelTransformer.SetLevel(definition);

                _writer.WriteCode(Code.Data, "\tdata\n\n");
                _writer.WriteCode(Code.Normal, "; Enable conditional code for coppershade");
                _writer.WriteCode(Code.Normal, "COPPERSHADE=\t\t1\n");
                _writer.WriteCode(Code.Normal, $"CS_FLICKER=\t\t{(_definition.Background.CopperShade.Flicker?1:0)}\n");

                OutputColors();
                OutputShadeLookup();
            }
            else
            {
                _writer.WriteCode(Code.Normal, "COPPERSHADE=\t\t0\n\n");
            }
        }

        private void OutputShadeLookup()
        {
            // Shade is bidirectional. All colors are compacted up and down so the range:

            // range = height-colors.length*2
            // margin = colors.length
            // delta =(shipY/height*range+margin)/colors.length

            var lookup = new StringBuilder();

            lookup.Append("ShadeLookup:");

            var height = (double)_levelTransformer.LevelHeight;
            var colorsCount = (double)_definition.Background.CopperShade.Colors.Length;
            var margin = colorsCount;
            var range = height - (colorsCount * 2);
            for (var y = 0; y < height; y++)
            {
                lookup.Append(y % 8 == 0 ? "\n\tdc.w\t\t" : ", ");
                var delta = ((y / height * range) + margin)/colorsCount;
                var deltaWord = (ushort)(delta * 256);
                lookup.Append($"${deltaWord:X}");
            }

            lookup.Append("\n");
            _writer.WriteCode(Code.Data, lookup.ToString());
        }

        private void OutputColors()
        {
            var colors = string.Join(",", _definition.Background.CopperShade.Colors.Select(c => $"${c:X}"));
            _writer.WriteCode(Code.Normal, $"SHADE_COLORS_COUNT=\t\t{_definition.Background.CopperShade.Colors.Length}");
            _writer.WriteCode(Code.Data, $"ShadeColors:\n\tdc.w\t\t{colors}");
        }

    }
}
