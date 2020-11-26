using System;
using System.Collections.Generic;
using ScrollerMapper.Converters;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.HeaderRenderers;

namespace ScrollerMapper.Processors
{
    internal class GameProcessor
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly ImageConverter _imageConverter;
        private readonly MusicConverter _musicConverter;
        private readonly HeaderRenderer _headerRenderer;
        private readonly IWriter _writer;

        public GameProcessor(IEnumerable<IProcessor> processors, ImageConverter imageConverter, MusicConverter musicConverter, 
            HeaderRenderer headerRenderer,
            IWriter writer)
        {
            _processors = processors;
            _imageConverter = imageConverter;
            _musicConverter = musicConverter;
            _headerRenderer = headerRenderer;
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
            
        }
        private readonly List<string> _fastHeaders = new List<string> { "menuMusic" };
        private readonly List<string> _chipHeaders = new List<string> { "menuSamples", "menu" };

        private void ProcessMenu(MenuDefinition definitionMenu)
        {
            _writer.StartDiskFile("menu");
            _headerRenderer.WriteHeader("Menu", ObjectType.Fast, _fastHeaders);
            _headerRenderer.WriteHeader("Menu", ObjectType.Chip, _chipHeaders);

            _imageConverter.ConvertAll("menu", definitionMenu.Background, Destination.Disk);
            _musicConverter.ConvertAll(definitionMenu.Music);

            _headerRenderer.WriteHeaderOffsets("Menu", ObjectType.Fast, _fastHeaders);
            _headerRenderer.WriteHeaderOffsets("Menu", ObjectType.Chip, _chipHeaders);

            var fastSize = _writer.GetCurrentOffset(ObjectType.Fast);
            var chipSize = _writer.GetCurrentOffset(ObjectType.Chip);
            _writer.WriteCode(Code.Normal, $"MENU_FAST_SIZE\tequ\t{fastSize}");
            _writer.WriteCode(Code.Normal, $"MENU_CHIP_SIZE\tequ\t{chipSize}");

            _writer.CompleteDiskFile();
        }

        private void ProcessLoadingScreen(LoadingScreenDefinition loadingScreenDefinition)
        {
            _imageConverter.ConvertAll("loading", loadingScreenDefinition.Image);
        }

        private void ProcessAllLevels(GameDefinition definition)
        {
            uint maxFastSize = 0;
            uint maxChipSize = 0;

            foreach (var levelTuple in definition.Levels)
            {
                var level = levelTuple.Value;
                _writer.StartDiskFile(levelTuple.Key);

                var levelDefinition = level.FileName.FromInputFolder().ReadJsonFile<LevelDefinition>();
                levelDefinition.Validate();
                foreach (var processor in _processors)
                {
                    processor.Process(levelDefinition);
                }

                var fastSize = _writer.GetCurrentOffset(ObjectType.Fast);
                maxFastSize = Math.Max(fastSize, maxFastSize);
                var chipSize = _writer.GetCurrentOffset(ObjectType.Chip);
                maxChipSize = Math.Max(chipSize, maxChipSize);

                _writer.CompleteDiskFile();
            }
            _writer.WriteCode(Code.Normal, $"LEVEL_FAST_SIZE\tequ\t{maxFastSize}");
            _writer.WriteCode(Code.Normal, $"LEVEL_CHIP_SIZE\tequ\t{maxChipSize}");
        }
    }
}
