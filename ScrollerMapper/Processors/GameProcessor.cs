using System;
using System.Collections.Generic;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.Converters;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal class GameProcessor
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly ImageConverter _imageConverter;
        private readonly MusicConverter _musicConverter;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly IWriter _writer;

        public GameProcessor(IEnumerable<IProcessor> processors, ImageConverter imageConverter, MusicConverter musicConverter, 
            SpriteRenderer spriteRenderer,
            IWriter writer)
        {
            _processors = processors;
            _imageConverter = imageConverter;
            _musicConverter = musicConverter;
            _spriteRenderer = spriteRenderer;
            _writer = writer;
        }

        public void Process(GameDefinition definition)
        {
            ProcessGame(definition);
            ProcessAllLevels(definition);
        }

        private void ProcessGame(GameDefinition definition)
        {
            if (definition.LoadingScreen != null)
            {
                ProcessLoadingScreen(definition.LoadingScreen);
            }
            else
            {
                throw new ConversionException("Must define a loadingScreen");
            }

            if (definition.Menu != null)
            {
                ProcessMenu(definition.Menu);
            }
            else
            {
                throw new ConversionException("Must define a menu");
            }

            if (definition.Data != null)
            {
                ProcessData(definition.Data);
            }
        }

        private void ProcessData(GameDataDefinition dataDefinition)
        {
            _writer.StartDiskFile("data");
            if (dataDefinition.Sprites != null)
            {
                foreach (var sprite in dataDefinition.Sprites)
                {
                    _spriteRenderer.Render(sprite.Key+"Sprite", sprite.Value, true);
                }
            }
            _writer.CompleteDiskFile();
        }

        private void ProcessMenu(MenuDefinition definitionMenu)
        {
            _writer.StartDiskFile("menu");
            _imageConverter.ConvertAll("menu", definitionMenu.Background, true);
            _musicConverter.ConvertAll(definitionMenu.Music);
            _writer.CompleteDiskFile();
        }

        private void ProcessLoadingScreen(LoadingScreenDefinition loadingScreenDefinition)
        {
            _imageConverter.ConvertAll("loading", loadingScreenDefinition.Image);
        }

        private void ProcessAllLevels(GameDefinition definition)
        {
            foreach (var levelTuple in definition.Levels)
            {
                var level = levelTuple.Value;

                var levelDefinition = level.FileName.FromInputFolder().ReadJsonFile<LevelDefinition>();
                levelDefinition.Validate();
                foreach (var processor in _processors)
                {
                    processor.Process(levelDefinition);
                }
            }
        }
    }
}
