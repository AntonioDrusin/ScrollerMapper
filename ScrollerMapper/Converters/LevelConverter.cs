using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Converters
{
    internal class LevelConverter 
    {
        private readonly Options _options;
        private readonly TiledConverter _tiledConverter;
        private readonly ImageConverter _imageConverter;
        private readonly BobConverter _bobConverter;

        public LevelConverter(
            Options options, 
            TiledConverter tiledConverter,
            ImageConverter imageConverter, 
            BobConverter bobConverter
            )
        {
            _options = options;
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
        }

        public void ConvertAll()
        {
            var definition = _options.InputFile.ReadJsonFile<LevelDefinition>();
            foreach (var tiledDefinition in definition.Tiles)
            {
                _tiledConverter.ConvertAll(tiledDefinition.Key, tiledDefinition.Value);
            }

            foreach (var imageDefinition in definition.Images)
            {
                _imageConverter.ConvertAll(imageDefinition.Key, imageDefinition.Value);
            }

            foreach (var bob in definition.Bobs)
            {
                _bobConverter.ConvertAll(bob.Key, bob.Value);
            }
        }
    }
}
