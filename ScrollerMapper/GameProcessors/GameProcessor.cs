using System;
using System.Collections.Generic;
using System.Linq;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.Converters;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.MiscRenderers;
using ScrollerMapper.Processors;
using ScrollerMapper.Writers;

namespace ScrollerMapper.GameProcessors
{
    internal class GameProcessor : IGameProcessor
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly ImageConverter _imageConverter;
        private readonly MusicConverter _musicConverter;
        private readonly HeaderRenderer _headerRenderer;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly IWriter _writer;
        private readonly ICodeWriter _codeWriter;
        private readonly ItemManager _items;
        private readonly BobConverter _bobConverter;

        public GameProcessor(IEnumerable<IProcessor> processors, ImageConverter imageConverter,
            MusicConverter musicConverter,
            HeaderRenderer headerRenderer,
            IWriter writer,
            ICodeWriter codeWriter,
            ItemManager items, BobConverter bobConverter, SpriteRenderer spriteRenderer)
        {
            _processors = processors;
            _imageConverter = imageConverter;
            _musicConverter = musicConverter;
            _headerRenderer = headerRenderer;
            _writer = writer;
            _codeWriter = codeWriter;
            _items = items;
            _bobConverter = bobConverter;
            _spriteRenderer = spriteRenderer;
        }

        public void Process(GameDefinition definition)
        {
            ProcessGame(definition);
            ProcessAllLevels(definition);
            ProcessSprites(definition.Sprites);
        }

        private void ProcessGame(GameDefinition definition)
        {
            if (definition.LoadingScreen != null)
            {
                ProcessLoadingScreen(definition.LoadingScreen);
            }
        

            if (definition.Menu != null)
            {
                ProcessMenu(definition.Menu);
            }
       

            if ( definition.Panel != null ) 
                ProcessPanel(definition.Panel);
        }

        private void ProcessPanel(GamePanelDefinition panel)
        {
            _codeWriter.WriteNumericConstant("LIVES_X",panel.Lives.X);
            _codeWriter.WriteNumericConstant("LIVES_Y", panel.Lives.Y);
            _codeWriter.WriteNumericConstant("LIVES_MAX", panel.Lives.Max);
            _codeWriter.WriteNumericConstant("LIVES_START", panel.Lives.Start);
            _codeWriter.WriteNumericConstant("LIVES_PIXEL_WIDTH", panel.Lives.Bob.Width);
            _codeWriter.WriteNumericConstant("LIVES_PIXEL_HEIGHT", panel.Lives.Bob.Height.Value);



            var bobPalette = panel.Palette.FromInputFolder().LoadIndexedBitmap();
            _bobConverter.ConvertBob(
                "life", 
                panel.Lives.Bob, 
                panel.PlaneCount, 
                bobPalette.Palette, 
                BobMode.NoColorFlip,
                Destination.Executable);
        }

        private readonly List<string> _fastHeaders = new List<string> {"menuMusic"};
        private readonly List<string> _chipHeaders = new List<string> {"menuSamples", "menu"};

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
            
            _codeWriter.WriteNumericConstant("MENU_FAST_SIZE",fastSize);
            _codeWriter.WriteNumericConstant("MENU_CHIP_SIZE",chipSize);

            _writer.CompleteDiskFile();
        }

        private void ProcessLoadingScreen(LoadingScreenDefinition loadingScreenDefinition)
        {
            _imageConverter.ConvertAll("loading", loadingScreenDefinition.Image);
        }

        private void ProcessAllLevels(GameDefinition definition)
        {
            if (definition.Levels == null) return;

            List<string> levelHeaders = new List<string> {"BobPalette", "BobArray", "Waves", "Bonuses"};

            uint maxFastSize = 0;
            uint maxChipSize = 0;

            foreach (var levelTuple in definition.Levels)
            {
                var level = levelTuple.Value;
                _writer.StartDiskFile(levelTuple.Key);
                _headerRenderer.WriteHeader("Level", ObjectType.Fast, levelHeaders);

                var levelDefinition = level.FileName.FromInputFolder().ReadJsonFile<LevelDefinition>();
                levelDefinition.Validate();
                List<IProcessor> processors = _processors.OrderBy(p => p.GetType().Name).ToList();

                while (processors.Count > 0)
                {
                    IProcessor processor;
                    try
                    {
                        processor = processors.First(p =>
                            p.RequiredTypes() == null || _items.HasAll(p.RequiredTypes()));
                    }
                    catch
                    {
                        var issues = string.Join("\n", processors.Select(p =>
                            $"{p.GetType().Name} Cannot be processed, it depends on unresolved objects: {string.Join(",", p.RequiredTypes().Except(_items.AvailableTypes()))}")
                        );
                        throw new ConversionException(issues);
                    }

                    Console.WriteLine($"PROCESSING {processor.GetType().Name}");
                    processor.Process(levelDefinition);
                    processors.Remove(processor);
                }


                var fastSize = _writer.GetCurrentOffset(ObjectType.Fast);
                maxFastSize = Math.Max(fastSize, maxFastSize);
                var chipSize = _writer.GetCurrentOffset(ObjectType.Chip);
                maxChipSize = Math.Max(chipSize, maxChipSize);

                _headerRenderer.WriteHeaderOffsets("Level", ObjectType.Fast, levelHeaders);
                _writer.CompleteDiskFile();
            }

            _codeWriter.WriteNumericConstant("LEVEL_FAST_SIZE",maxFastSize);
            _codeWriter.WriteNumericConstant("LEVEL_CHIP_SIZE",maxChipSize);

        }


        private void ProcessSprites(Dictionary<string, SpriteDefinition> sprites)
        {
            if (sprites != null)
            {
                foreach (var keyValue in sprites)
                {
                    _spriteRenderer.Render(keyValue.Key, keyValue.Value);
                }
            }
        }
    }
}