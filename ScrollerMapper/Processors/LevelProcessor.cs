using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ScrollerMapper.Converters;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;
using ImageConverter = ScrollerMapper.Converters.ImageConverter;

namespace ScrollerMapper.Processors
{
    internal class LevelProcessor : IProcessor
    {
        const int BytesPerRow = 40;
        const int ScreenHeight = 256;

        private readonly TiledConverter _tiledConverter;
        private readonly ImageConverter _imageConverter;
        private readonly BobConverter _bobConverter;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly IWriter _writer;
        private readonly ItemManager _items;
        private readonly ICodeWriter _codeWriter;

        private int _scoreboardHeight;
        private LevelDefinition _definition;

        public LevelProcessor(
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter,
            IPaletteRenderer paletteRenderer,
            IWriter writer,
            ItemManager items,
            ICodeWriter codeWriter
        )
        {
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
            _writer = writer;
            _items = items;
            _codeWriter = codeWriter;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;

            if (_definition.Tiles != null)
            {
                ConvertTiles(_definition);
            }

            var bobPalette = _definition.BobPaletteFile.FromInputFolder().LoadIndexedBitmap();

            if (_definition.SpritePaletteFile != null)
            {
                var paletteBitmap = _definition.SpritePaletteFile.FromInputFolder().LoadIndexedBitmap();

                var palette = new PaletteTransformer("sprite", paletteBitmap.Palette, 16);
                _paletteRenderer.Render(palette, false);

                if (_definition.Player != null)
                {
                    if (_definition.Player.Shots == null)
                    {
                        throw new ConversionException("Must define 'shots' for 'player'");
                    }

                    if (_definition.Player.Shots.Count != 6)
                    {
                        throw new ConversionException("Must define exactly 2 'shots' for 'player'");
                    }

                    int i = 0;
                    foreach (var shot in _definition.Player.Shots)
                    {
                        if (shot.Bob == null)
                            throw new ConversionException("Must define 'bob' for each of the 'player.shots'");
                        _bobConverter.ConvertBob($"shot{i}", shot.Bob, definition.BobPlaneCount, bobPalette.Palette,
                            _definition.BobPaletteFlip0AndLast ? BobMode.ColorFlip : BobMode.NoColorFlip, Destination.Executable);
                        i++;
                    }

                    int maxCount = 0;
                    i = 0;
                    foreach (var shot in _definition.Player.Shots)
                    {
                        var soundOffset = _items.Get(ItemTypes.Sound, shot.Sound, "Player shots").Offset;
                        _codeWriter.WriteStructValue($"shot{i}", new ShotStructure
                        {
                            ShotBobPtr = $"shot{i}Bob", 
                            ShotVX = (short)shot.Vx,
                            ShotHit = (short)shot.Hit,
                            ShotCooldown = (short)shot.Cooldown,
                            ShotMax = (short)shot.MaxCount,
                            ShotSound = (short)soundOffset,
                        });
                        if (shot.MaxCount > maxCount) maxCount = shot.MaxCount;
                        i++;
                    }

                    _codeWriter.WriteNumericConstant("MAX_BULLETS", maxCount);

                    var playerVx = _definition.Player.Vx;
                    var playerVy = _definition.Player.Vy;
                    var playerVxy = Math.Sin(Math.PI / 4) * (playerVx + playerVy) / 2;
                    _codeWriter.WriteNumericConstant("PLAYER_VX", playerVx);
                    _codeWriter.WriteNumericConstant("PLAYER_VY", playerVy);
                    _codeWriter.WriteNumericConstant("PLAYER_VD", (int)playerVxy);
                    
                    _codeWriter.WriteNumericConstant("PLAYER_RAYDURATION", _definition.Player.Death.RayDuration);
                    _codeWriter.WriteNumericConstant("PLAYER_SPAWNDELAY", _definition.Player.Death.SpawnDelay);
                    _codeWriter.WriteNumericConstant("PLAYER_SPAWNX", _definition.Player.Death.Spawn.X);
                    _codeWriter.WriteNumericConstant("PLAYER_SPAWNY", _definition.Player.Death.Spawn.Y);
                    _codeWriter.WriteNumericConstant("PLAYER_SPAWNCELH", _definition.Player.Death.SpawnCelH);
                    _codeWriter.WriteNumericConstant("PLAYER_SPAWNCELV", _definition.Player.Death.SpawnCelV);
                    _codeWriter.WriteNumericConstant("PLAYER_INVULNDURATION", _definition.Player.Death.InvulnerabilityDuration);
                }
            }
            else if (_definition.Player != null)
            {
                throw new ConversionException("You must specify a SpritePaletteFile to have a sprite.");
            }

            if (_definition.Images != null)
            {
                foreach (var imageDefinition in _definition.Images)
                {
                    _imageConverter.ConvertAll(imageDefinition.Key, imageDefinition.Value);
                }
            }

            if (_definition.Panel != null)
            {
                _imageConverter.ConvertAll("ScoreFont", _definition.Panel.Font);
                _codeWriter.WriteIncludeComments("Score location");
                
                _codeWriter.WriteNumericConstant("SCORE_X", _definition.Panel.X);
                _codeWriter.WriteNumericConstant("SCORE_Y", _definition.Panel.Y);

                var scoreboardInfo = _imageConverter.ConvertAll("Scoreboard", _definition.Panel.Scoreboard);
                _scoreboardHeight = scoreboardInfo.Height;
            }

            _codeWriter.WriteNumericConstant("LEVEL_WIDTH",_definition.Level.Width);
            _codeWriter.WriteIncludeComments("Amount to shift a levelwide X coordinates before using the MapXLookup");
            _codeWriter.WriteNumericConstant("FXP_SHIFT",_definition.FixedPointBits);

            WriteMapLookup();
            WriteMainLookup();
            WriteBobList();
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Bob;
            yield return ItemTypes.Path;
            yield return ItemTypes.Enemy;
            yield return ItemTypes.Sound;
        }

        private void ConvertTiles(LevelDefinition definition)
        {
            foreach (var tiledDefinition in definition.Tiles)
            {
                _tiledConverter.ConvertAll(tiledDefinition.Key, tiledDefinition.Value);
            }
        }
        
        private void WriteMapLookup()
        {
            var xShift = (int) (Math.Log(_definition.Level.Width / 256.0, 2) + 0.5);
            
            _codeWriter.WriteIncludeComments("Amount to shift a levelwide X coordinates before using the MapXLookup");
            _codeWriter.WriteNumericConstant("MAP_XSHIFT", xShift);


            var shiftedLevelWidth = _definition.Level.Width >> xShift;
            var lookup = new List<short>();
            for (int x = 0; x < 256; x++)
            {
                var mapX = (short)(x * _definition.Panel.Map.Width / shiftedLevelWidth + _definition.Panel.Map.X);
                lookup.Add(mapX);
            }
            _codeWriter.WriteIncludeComments("given X>>FXP_SHIFT returns x coordinate for the point in the map");
            _codeWriter.WriteArray("MapXLookup", 16, lookup);

            var levelHeight = ScreenHeight - _scoreboardHeight;
            var mapHeight = _definition.Panel.Map.Height;
            lookup.Clear();
            for (int y = 0; y < 256; y++)
            {
                var mappedY = (y + _definition.Panel.Map.Y) * mapHeight / levelHeight;
                var offset = (short)(mappedY * BytesPerRow * _definition.Panel.Scoreboard.PlaneCount);
                lookup.Add(offset);
            }

            _codeWriter.WriteIncludeComments("given the Y returns the offset form the beginning of the score bitmap");
            _codeWriter.WriteArray("MapYLookup", 16, lookup);
        }

        private void WriteMainLookup()
        {
            var random = new Random();

            if (_definition.MainHorizontalBorder % 8 != 0)
                throw new ConversionException("MainHorizontalBorder must be multiple of 8");
            
            _codeWriter.WriteNumericConstant("MAIN_BORDERHB", _definition.MainHorizontalBorder / 8);
            _codeWriter.WriteNumericConstant("MAIN_BORDERH", _definition.MainHorizontalBorder);
            _codeWriter.WriteNumericConstant("MAIN_BORDERV", _definition.MainVerticalBorder);

            var oneRowModulo = 40 + 2 * _definition.MainHorizontalBorder / 8;
            var lookup = new List<short>();
            for (var y = 0; y < 256; y++)
            {
                var r = random.Next(0, 3);
                var offset = (short)(y * oneRowModulo * 4 + r * oneRowModulo);
                lookup.Add(offset);
            }

            _codeWriter.WriteIncludeComments("given the Y returns the offset form the beginning of the score bitmap");
            _codeWriter.WriteArray("MainYRandomLookup", 16, lookup);
        }

        private void WriteBobList()
        {
            var bobs = _items.Get(ItemTypes.Bob);
            _writer.StartObject(ObjectType.Fast, "BobArray");
            _writer.WriteWord((ushort) (bobs.Count - 1));
            foreach (var bob in bobs.OrderBy(b => b.Index))
            {
                _writer.WriteOffset(ObjectType.Chip, bob.Offset);
            }

            _writer.EndObject();
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    internal class ShotStructure
    {
        public string ShotBobPtr;
        public short ShotVX;
        public short ShotHit;
        public short ShotMax;
        public short ShotCooldown;
        public short ShotSound;
    }
}