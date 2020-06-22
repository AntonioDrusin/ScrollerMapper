using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.Converters
{
    internal class LevelConverter
    {
        const int BytesPerRow = 40;
        const int ScreenHeight = 256;

        private readonly Options _options;
        private readonly TiledConverter _tiledConverter;
        private readonly ImageConverter _imageConverter;
        private readonly BobConverter _bobConverter;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly IWriter _writer;
        private Dictionary<string, BobInfo> _bobs;
        private int _bobIndex;
        private Dictionary<string, PathInfo> _paths;
        private Dictionary<string, EnemyInfo> _enemies;
        private int _scoreboardHeight;
        private LevelDefinition _definition;

        public LevelConverter(
            Options options,
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter,
            IPaletteRenderer paletteRenderer,
            SpriteRenderer spriteRenderer,
            IWriter writer)
        {
            _options = options;
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
            _spriteRenderer = spriteRenderer;
            _writer = writer;
            _bobs = new Dictionary<string, BobInfo>();
        }

        public void ConvertAll()
        {
            _definition = _options.InputFile.ReadJsonFile<LevelDefinition>();
            _definition.Validate();
            if (_definition.Tiles != null)
            {
                ConvertTiles(_definition);
            }

            var bobPalette = _definition.BobPaletteFile.FromInputFolder().LoadIndexedBitmap();

            if (_definition.SpritePaletteFile != null)
            {
                var paletteBitmap = _definition.SpritePaletteFile.FromInputFolder().LoadIndexedBitmap();

                _paletteRenderer.Render("sprite", paletteBitmap.Palette, 16);

                if (_definition.Player != null)
                {
                    _spriteRenderer.Render("player", _definition.Player.MainSprite);
                    if (_definition.Player.Shots == null)
                        throw new ConversionException("Must define 'shots' for 'player'");
                    if (_definition.Player.Shots.Bob == null)
                        throw new ConversionException("Must define 'main' for 'player.shots'");

                    ConvertBob("shot", _definition.Player.Shots.Bob, _definition, bobPalette);
                    _writer.WriteCode(Code.Normal, $"BULLET_VX\t\tequ\t{(int) (_definition.Player.Shots.Vx)}");
                    _writer.WriteCode(Code.Normal, $"BULLET_COOLDOWN\t\tequ\t{_definition.Player.Shots.Cooldown}");
                    _writer.WriteCode(Code.Normal, $"MAX_BULLETS\t\tequ\t{_definition.Player.Shots.MaxCount}");
                    var playerVx = (int) (_definition.Player.Vx.GetValueOrDefault(32));
                    var playerVy = (int) (_definition.Player.Vy.GetValueOrDefault(32));
                    var playerVxy = Math.Sin(Math.PI / 4) * (playerVx + playerVy) / 2;
                    _writer.WriteCode(Code.Normal, $"PLAYER_VX\t\tequ\t{playerVx}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_VY\t\tequ\t{playerVy}");
                    _writer.WriteCode(Code.Normal, $"PLAYER_VD\t\tequ\t{(int) playerVxy}");
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

            // Move all of this in its own?
            WriteBobComments();

            ConvertBobPalette(bobPalette.Palette);

            foreach (var bob in _definition.Bobs)
            {
                ConvertBob(bob.Key, bob.Value, _definition, bobPalette);
            }

            _writer.WriteCode(Code.Normal, "\n\n");

            _writer.WriteCode(Code.Normal, $"LEVEL_WIDTH\t\tequ\t\t{_definition.Level.Width}");
            _writer.WriteCode(Code.Normal,
                $"FXP_SHIFT\t\tequ\t\t{_definition.FixedPointBits}\t; Amount to shift a levelwide X coordinates before using the MapXLookup");


            WriteMapLookup();
            WriteMainLookup();
            WriteEnemies(_definition);
            WritePaths(_definition.Paths);
            WriteWaves(_definition);
            WriteBobList();
        }

        private void ConvertBob(string name, BobDefinition bob, LevelDefinition definition, Bitmap bobPalette)
        {
            _bobConverter.ConvertAll(name, bob, definition.BobPlaneCount, bobPalette.Palette);
            _bobs.Add(name, new BobInfo {Index = _bobIndex++, Name = name});
        }

        private void ConvertTiles(LevelDefinition definition)
        {
            foreach (var tiledDefinition in definition.Tiles)
            {
                _tiledConverter.ConvertAll(tiledDefinition.Key, tiledDefinition.Value);
            }
        }

        private void WriteEnemies(LevelDefinition definition)
        {
            WriteEnemyComments();
            _writer.WriteCode(Code.Normal, "Enemies:");
            _enemies = new Dictionary<string, EnemyInfo>();

            var index = 0;
            var offset = 0;
            foreach (var enemyKeyValue in definition.Enemies)
            {
                BobInfo bobForEnemy;
                var enemy = enemyKeyValue.Value;
                try
                {
                    bobForEnemy = _bobs[enemy.Bob];
                }
                catch (KeyNotFoundException)
                {
                    throw new ConversionException($"Bob '{enemy.Bob}' for enemy '{enemyKeyValue.Key}' was not found.");
                }

                _writer.WriteCode(Code.Normal, $"\tdc.l\t{bobForEnemy.Name}Bob");
                _writer.WriteCode(Code.Normal, $"\tdc.w\t{enemy.FrameDelay}\t\t;Cel period");
                _writer.WriteCode(Code.Normal, $"\tdc.w\t{enemy.Points}\t\t;Points");
                _enemies.Add(enemyKeyValue.Key,
                    new EnemyInfo {Name = enemyKeyValue.Key, Index = index++, Offset = offset});
                offset += 8;
            }

            _writer.WriteCode(Code.Normal, "\n");
        }

        private void WriteEnemyComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for Enemies
** EnemyBobOffset is an offset in bytes from the Enemies label
    structure   EnemyStructure, 0
    long        EnemyBobPtr_l
    word        EnemyPeriod_w       ; Period in frames between switching bobs
    word        EnemyPoints_w
    label       ENEMY_STRUCT_SIZE
");
        }

        private void WritePaths(Dictionary<string, PathDefinition> definitionPaths)
        {
            _paths = new Dictionary<string, PathInfo>();
            WritePathComments();
            var firstTransformer = new SmoothInputPathTransformer();
            var secondTransformer = new OutputPathCoalesceTransformer();

            _writer.WriteCode(Code.Normal, $"Paths:");
            var offset = 0;
            var index = 0;
            foreach (var path in definitionPaths)
            {
                var finalPath = firstTransformer.TransformPath(path.Value.Steps);
                finalPath = secondTransformer.GroupPath(finalPath);

                _paths.Add(path.Key, new PathInfo {Name = path.Key, Offset = offset, Index = index++});
                _writer.WriteCode(Code.Normal, $"; path '{path.Key}', offset {offset}");
                foreach (var step in finalPath)
                {
                    _writer.WriteCode(Code.Normal,
                        $"\t\tdc.w\t\t{step.FrameCount},{step.VelocityX},{step.VelocityY}");
                    offset += 6;
                }

                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t0, 0, 0");
                offset += 6;
            }
        }

        private void WritePathComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for a path
** The structure is repeated until the FrameCount is 0. That is the end of the path. Enemy will disappear.
** Each path is formed by a number of these structure until framecount is 0.

    structure       PathStructure, 0
    word            PathFrameCount_w
    word            PathVX_w
    word            PathVY_w
    label           PATH_STRUCT_SIZE

");
        }

        private void WriteWaves(LevelDefinition definition)
        {
            WriteWaveComments();
            _writer.WriteCode(Code.Normal, $"MaxActiveWaves\t\tequ\t{definition.MaxActiveWaves}");
            _writer.WriteCode(Code.Normal, $"MaxActiveEnemies\t\tequ\t{definition.MaxActiveEnemies}");
            _writer.WriteCode(Code.Normal, "Waves:");
            foreach (var wavePair in definition.Waves)
            {
                var wave = wavePair.Value;
                var path = GetPathFor(wave.Path, wavePair.Key);
                var enemy = GetEnemyFor(wave.Enemy, wavePair.Key);

                _writer.WriteCode(Code.Normal, $"; wave '{wavePair.Key}'");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.FrameDelay}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.OnExistingWaves}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.Count}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.l\t\tEnemies+{enemy.Offset}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.l\t\tPaths+{path.Offset}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.Period}");

                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.StartX},{wave.StartY}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.StartXOffset},{wave.StartYOffset}");
            }

            _writer.WriteCode(Code.Normal, "; final wave has a special WaveDelay of $ffff to mark the end");
            _writer.WriteCode(Code.Normal, "\t\tdc.w\t\t$ffff\t\t");
        }

        private PathInfo GetPathFor(string pathName, string sourceName)
        {
            PathInfo path;
            try
            {
                path = _paths[pathName];
            }
            catch (KeyNotFoundException)
            {
                throw new ConversionException($"Cannot find path '{pathName}' for '{sourceName}'");
            }

            return path;
        }

        private EnemyInfo GetEnemyFor(string enemyName, string sourceName)
        {
            EnemyInfo enemy;
            try
            {
                enemy = _enemies[enemyName];
            }
            catch (KeyNotFoundException)
            {
                throw new ConversionException($"Cannot find enemy '{enemyName}' for '{sourceName}'");
            }

            return enemy;
        }

        private void WriteWaveComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for wave
** WaveEnemyOffset_b is an offset off of the Enemies label to point to the enemy
    structure   WaveStructure, 0
    word        WaveDelay_w         ; Frame delay before wave is considered for spawn
    word        WaveOnCount_w       ; no more than OnCount waves remaining before start
    word        WaveEnemyCount_w    
    long        WaveEnemyPtr_l      
    long        WavePathPtr_l           
    word        WavePeriod_w        ; Frames between enemy spawn
    word        WaveSpawnX_w        ; spawn location X
    word        WaveSpawnY_w        ; spawn location Y
    word        WaveSpawnXOffset_w   
    word        WaveSpawnYOffset_w  
    label       WAVE_STRUCT_SIZE
");
        }

        private void WriteBobComments()
        {
            _writer.WriteCode(Code.Normal, "");
            _writer.WriteCode(Code.Normal, "** Structure of BOBS ");
            _writer.WriteCode(Code.Normal, "** WordWidth  WORD ");
            _writer.WriteCode(Code.Normal, "** BobCount WORD ");
            _writer.WriteCode(Code.Normal, "**  ");
            _writer.WriteCode(Code.Normal, "** BobCount frames follow: ");
            _writer.WriteCode(Code.Normal, "** FrameByteOffset WORD ; from the beginning of the file ");
            _writer.WriteCode(Code.Normal, "** MaskByteOffset WORD ; from the beginning of the file ");
            _writer.WriteCode(Code.Normal, "** Lines WORD ; how many lines is this frame made out of ");
            _writer.WriteCode(Code.Normal, "** YAdjustment WORD ; word to add to Y when drawing ");
            _writer.WriteCode(Code.Normal, "**  ");
            _writer.WriteCode(Code.Normal, "**  Binary Blob data follows ");
            _writer.WriteCode(Code.Normal, "**  @ FrameByteOffset interleaved planes with the data");
            _writer.WriteCode(Code.Normal,
                "**  @ MaskByteOffset interleaved planes with the data (same mask is repeated for each plane)");
            _writer.WriteCode(Code.Normal, @"
    structure   BobsStructure, 0
    word        BobsWordWidth_w
    word        BobsCount_w 
    label       BOBS_STRUCT_SIZE

    structure   BCelStructure, 0 
    long        BCelPlaneOffset_l        ; Long, so upon load you can turn it into a pointer
    long        BCelMaskOffset_l         ; Long, so upon load you can turn this into a pointer
    word        BCelHeight_w
    word        BCelYAdjust_w
    word        BCelDModulo_w            ; Destination modulo for bob (set to 0, you need to initialize this)
    word        BCelBlitSize_w           ; This is pre-calculated    
    label       BCEL_STRUCT_SIZE

");
            _writer.WriteCode(Code.Normal, "");
        }

        private void ConvertBobPalette(ColorPalette palette)
        {
            _paletteRenderer.Render("bob", palette, _definition.BobPlaneCount.PowerOfTwo());
        }

        private void WriteMapLookup()
        {
            var xShift = (int) (Math.Log(_definition.Level.Width / 256.0, 2) + 0.5);
            _writer.WriteCode(Code.Normal,
                $"MAP_XSHIFT\t\tequ\t\t{xShift}\t; Amount to shift a levelwide X coordinates before using the MapXLookup");

            _writer.WriteCode(Code.Normal,
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


            _writer.WriteCode(Code.Normal, lookup.ToString());
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

            _writer.WriteCode(Code.Normal, lookup.ToString());
        }

        private void WriteBobList()
        {
            _writer.WriteCode(Code.Normal, "\tsection\tdata");
            _writer.WriteCode(Code.Normal, $"\nBOB_COUNT\t\tequ\t{_bobs.Count}");
            _writer.WriteCode(Code.Normal, "\n\n** Pointers to all loaded Bobs\n");
            _writer.WriteCode(Code.Normal, "BobPtrs:");
            foreach (var bob in _bobs.OrderBy(b => b.Value.Index))
            {
                _writer.WriteCode(Code.Normal, $"\tdc.l\t{bob.Value.Name}Bob");
            }
        }
    }
}