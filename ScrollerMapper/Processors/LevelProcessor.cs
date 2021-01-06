using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.Converters;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;
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
        private readonly SpriteRenderer _spriteRenderer;
        private readonly IWriter _writer;
        private readonly ItemManager _items;

        private int _scoreboardHeight;
        private LevelDefinition _definition;

        public LevelProcessor(
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter,
            IPaletteRenderer paletteRenderer,
            SpriteRenderer spriteRenderer,
            IWriter writer,
            ItemManager items
        )
        {
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
            _spriteRenderer = spriteRenderer;
            _writer = writer;
            _items = items;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;

            ProcessData(definition.Data); // Generic level data?

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
                    _writer.WriteCode(Code.Normal,
                        $"PLAYER_FRAMEDELAY\t\tequ\t{(uint) _definition.Player.MainSprite.Duration / 20}");
                    _spriteRenderer.Render("player", _definition.Player.MainSprite);
                    _spriteRenderer.Render("grazing", _definition.Player.GrazingSprite);

                    if (_definition.Player.Shots == null)
                    {
                        throw new ConversionException("Must define 'shots' for 'player'");
                    }

                    if (_definition.Player.Shots.Count != 6)
                    {
                        throw new ConversionException("Must define exactly 2 'shots' for 'player'");
                    }

                    WriteShotStructure();
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
                    _writer.WriteCode(Code.Data, "\tsection data");
                    _writer.WriteCode(Code.Data, "shots:");
                    foreach (var shot in _definition.Player.Shots)
                    {
                        _writer.WriteCode(Code.Data, $"shot{i}:");
                        _writer.WriteCode(Code.Data, $"\tdc.l\tshot{i}Bob");
                        _writer.WriteCode(Code.Data,
                            $"\tdc.w\t{shot.Vx}, {shot.Hit}, {shot.MaxCount}, {shot.Cooldown}\t;vx,hit,maxCount,cooldDown");


                        var soundOffset = _items.Get(ItemTypes.Sound, shot.Sound, "Player shots").Offset;

                        _writer.WriteCode(Code.Data, $"\tdc.w\t{soundOffset}\t; sound Offset");
                        if (shot.MaxCount > maxCount) maxCount = shot.MaxCount;
                        i++;
                    }

                    _writer.WriteCode(Code.Normal, $"MAX_BULLETS\t\tequ\t{maxCount}");

                    var playerVx = _definition.Player.Vx;
                    var playerVy = _definition.Player.Vy;
                    var playerVxy = Math.Sin(Math.PI / 4) * (playerVx + playerVy) / 2;
                    _writer.WriteCode(Code.Normal, $"PLAYER_VX\t\tequ\t{playerVx}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_VY\t\tequ\t{playerVy}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_VD\t\tequ\t{(int) playerVxy}");

                    _writer.WriteCode(Code.Normal,
                        $"PLAYER_RAYDURATION\t\tequ\t{_definition.Player.Death.RayDuration}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_SPAWNDELAY\t\tequ\t{_definition.Player.Death.SpawnDelay}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_SPAWNX\t\tequ\t{_definition.Player.Death.Spawn.X}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_SPAWNY\t\tequ\t{_definition.Player.Death.Spawn.Y}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_SPAWNCELH\t\tequ\t{_definition.Player.Death.SpawnCelH}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_SPAWNCELV\t\tequ\t{_definition.Player.Death.SpawnCelV}");
                    _writer.WriteCode(Code.Normal,
                        $"PLAYER_INVULNDURATION\t\tequ\t{_definition.Player.Death.InvulnerabilityDuration}");
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
                _writer.WriteCode(Code.Normal, $"; Score location");
                _writer.WriteCode(Code.Normal, $"SCORE_X\t\tequ\t{_definition.Panel.X}");
                _writer.WriteCode(Code.Normal, $"SCORE_Y\t\tequ\t{_definition.Panel.Y}");

                var scoreboardInfo = _imageConverter.ConvertAll("Scoreboard", _definition.Panel.Scoreboard);
                _scoreboardHeight = scoreboardInfo.Height;
            }

            _writer.WriteCode(Code.Normal, $"LEVEL_WIDTH\t\tequ\t\t{_definition.Level.Width}");
            _writer.WriteCode(Code.Normal,
                $"FXP_SHIFT\t\tequ\t\t{_definition.FixedPointBits}\t; Amount to shift a levelwide X coordinates before using the MapXLookup");


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

        private void WriteShotStructure()
        {
            _writer.WriteCode(Code.Normal, @"
;---- SHOTS STRUCTURE ----
    structure   ShotStructure, 0
    long        ShotBobPtr_l
    word        ShotVX_w
    word        ShotHit_w
    word        ShotMax_w
    word        ShotCooldown_w
    word        ShotSound_w             ; Offset in the sounds table
    label       SHOT_STRUCT_SIZE
");
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
            _writer.WriteCode(Code.Normal,
                $"MAP_XSHIFT\t\tequ\t\t{xShift}\t; Amount to shift a levelwide X coordinates before using the MapXLookup");

            _writer.WriteCode(Code.Data, "\tsection\tdata");
            _writer.WriteCode(Code.Data,
                "\n\nMapXLookup: ; given X>>FXP_SHIFT returns x coordinate for the point in the map");

            var shiftedLevelWidth = _definition.Level.Width >> xShift;
            var lookup = new StringBuilder();
            for (int x = 0; x < 256; x++)
            {
                int mapX = x * _definition.Panel.Map.Width / shiftedLevelWidth + _definition.Panel.Map.X;
                lookup.Append(x % 32 == 0 ? "\n\tdc.w\t" : ",");
                lookup.Append($"{mapX}");
            }

            var levelHeight = ScreenHeight - _scoreboardHeight;
            var mapHeight = _definition.Panel.Map.Height;
            lookup.Append(
                "\n\nMapYLookup: ; given the Y returns the offset form the beginning of the score bitmap");
            for (int y = 0; y < 256; y++)
            {
                var mappedY = (y + _definition.Panel.Map.Y) * mapHeight / levelHeight;
                var offset =
                    mappedY * BytesPerRow * _definition.Panel.Scoreboard.PlaneCount;
                lookup.Append(y % 32 == 0 ? "\n\tdc.w\t" : ",");
                lookup.Append($"{offset}");
            }

            _writer.WriteCode(Code.Data, lookup.ToString());
        }

        private void WriteMainLookup()
        {
            var random = new Random();
            var lookup = new StringBuilder();

            if (_definition.MainHorizontalBorder % 8 != 0)
                throw new ConversionException("MainHorizontalBorder must be multiple of 8");
            _writer.WriteCode(Code.Normal, $"MAIN_BORDERHB=\t\t{_definition.MainHorizontalBorder / 8}");
            _writer.WriteCode(Code.Normal, $"MAIN_BORDERH=\t\t{_definition.MainHorizontalBorder}");
            _writer.WriteCode(Code.Normal, $"MAIN_BORDERV=\t\t{_definition.MainVerticalBorder}");

            lookup.Append(
                "\n\nMainYRandomLookup: ; given the Y returns the offset form the beginning of the score bitmap");
            var oneRowModulo = 40 + 2 * _definition.MainHorizontalBorder / 8;
            for (var y = 0; y < 256; y++)
            {
                var r = random.Next(0, 3);
                var offset = y * oneRowModulo * 4 + r * oneRowModulo;
                lookup.Append(y % 32 == 0 ? "\n\tdc.w\t" : ",");
                lookup.Append($"{offset}");
            }

            _writer.WriteCode(Code.Data, "\tsection\tdata");
            _writer.WriteCode(Code.Data, lookup.ToString());
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


        private void ProcessData(DataDefinition dataDefinition)
        {
            var spriteOffsets = new List<uint>();

            if (dataDefinition?.Sprites != null)
            {
                int i = 0;
                foreach (var sprite in dataDefinition.Sprites)
                {
                    spriteOffsets.Add(_writer.GetCurrentOffset(ObjectType.Chip));
                    _spriteRenderer.Render(i + "Sprite", sprite, Destination.Disk);
                    i++;
                }

                _writer.StartObject(ObjectType.Fast, "SpriteArray"); // References to objects that are in chip.
                _writer.WriteWord((ushort) (spriteOffsets.Count - 1));
                foreach (var offset in spriteOffsets)
                {
                    _writer.WriteOffset(ObjectType.Chip, offset);
                }

                _writer.EndObject();
            }
        }
    }
}