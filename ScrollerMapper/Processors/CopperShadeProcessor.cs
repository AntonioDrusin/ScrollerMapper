using System.Collections.Generic;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class CopperShadeProcessor : IProcessor
    {
        private readonly LevelTransformer _levelTransformer;
        private readonly ICodeWriter _codeWriter;
        private LevelDefinition _definition;

        public CopperShadeProcessor(LevelTransformer levelTransformer, ICodeWriter codeWriter)
        {
            _levelTransformer = levelTransformer;
            _codeWriter = codeWriter;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;
            if (definition.Background?.CopperShade != null)
            {
                _levelTransformer.SetLevel(definition);

                _codeWriter.WriteNumericConstant("COPPERSHADE", 1);
                _codeWriter.WriteNumericConstant("CS_FLICKER", _definition.Background.CopperShade.Flicker ? 1 : 0);
                OutputColors();
                OutputShadeLookup();
            }
            else
            {
                _codeWriter.WriteNumericConstant("COPPERSHADE", 0);
            }
        }

        public IEnumerable<string> RequiredTypes()
        {
            return null;
        }

        private void OutputShadeLookup()
        {
            // Shade is bidirectional. All colors are compacted up and down so the range:

            // range = height-colors.length*2
            // margin = colors.length
            // delta =(shipY/height*range+margin)/colors.length

            var words = new List<ushort>();
            var height = (double)_levelTransformer.LevelHeight;
            var colorsCount = (double)_definition.Background.CopperShade.Colors.Length;
            var margin = colorsCount;
            var range = height - (colorsCount * 2);
            for (var y = 0; y < height; y++)
            {
                var delta = ((y / height * range) + margin) / colorsCount;
                var deltaWord = (ushort)(delta * 256);
                words.Add(deltaWord);
            }
            _codeWriter.WriteArray("ShadeLookup", 8, words);
        }

        private void OutputColors()
        {
            _codeWriter.WriteArray("ShadeColors", 8, _definition.Background.CopperShade.Colors);
            _codeWriter.WriteNumericConstant("SHADE_COLORS_COUNT", _definition.Background.CopperShade.Colors.Length);
        }
    }
}