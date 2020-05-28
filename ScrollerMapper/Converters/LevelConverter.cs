using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.Converters
{
    internal class LevelConverter
    {
        private readonly Options _options;
        private readonly TiledConverter _tiledConverter;
        private readonly ImageConverter _imageConverter;
        private readonly BobConverter _bobConverter;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly IWriter _writer;
        private Dictionary<string, BobInfo> _bobs;
        private Dictionary<string, PathInfo> _paths;
        private Dictionary<string, EnemyInfo> _enemies;

        public LevelConverter(
            Options options,
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter, IPaletteRenderer paletteRenderer, IWriter writer)
        {
            _options = options;
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
            _writer = writer;
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


            // Move all of this in its own?
            WriteBobComments();


            var bobPalette = definition.BobPaletteFile.FromInputFolder().LoadBitmap();
            ConvertBobPalette(bobPalette.Palette, definition);

            _bobs = new Dictionary<string, BobInfo>();
            var index = 0;
            foreach (var bob in definition.Bobs)
            {
                _bobConverter.ConvertAll(bob.Key, bob.Value, definition.BobPlaneCount, bobPalette.Palette);
                _bobs.Add(bob.Key, new BobInfo {Index = index++, Name = bob.Key});
            }

            _writer.WriteCode(Code.Normal, "\tsection\tdata");
            _writer.WriteCode(Code.Normal, "\n\n** Pointers to all loaded Bobs\n");
            _writer.WriteCode(Code.Normal, "BobPtrs:");
            foreach (var bob in _bobs.OrderBy(b=>b.Value.Index))
            {
                _writer.WriteCode(Code.Normal, $"\tdc.l\t{bob.Value.Name}Bob");
            }

            _writer.WriteCode(Code.Normal, "\n\n");

            WriteEnemies(definition);
            WritePaths(definition.Paths);
            WriteWaves(definition);


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

                _writer.WriteCode(Code.Normal, $"\tdc.b\t{bobForEnemy.Index*4},{enemy.FrameDelay}\t\t; Enemy {enemyKeyValue.Key} offset {offset} bob {enemy.Bob}");
                _writer.WriteCode(Code.Normal, $"\tdc.w\t{enemy.Points}");
                offset += 4;
                _enemies.Add(enemyKeyValue.Key, new EnemyInfo {Name = enemyKeyValue.Key, Index = index++});
            }

            _writer.WriteCode(Code.Normal, "\n\nEnemyPtr:");
            foreach (var enemy in _enemies)
            {
                _writer.WriteCode(Code.Normal, $"\t\tdc.l\tEnemies+{enemy.Value.Index*4}\t; {enemy.Key}");
            }
            _writer.WriteCode(Code.Normal, "\n");
        }

        private void WriteEnemyComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for Enemies
** EnemyBobOffset is an offset in bytes from the Enemies label

EnemyBobPtrOffset_b  equ     0   
EnemyPeriod_b        equ     1  ; Period in frames between switching bobs
EnemyPoints_w        equ     2
ENEMY_STRUCT_SIZE    equ     4
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

                _paths.Add(path.Key, new PathInfo { Name = path.Key, Offset = offset, Index = index++ });
                _writer.WriteCode(Code.Normal, $"; path '{path.Key}', offset {offset}");
                foreach (var step in finalPath)
                {
                    _writer.WriteCode(Code.Normal, $"\t\tdc.b\t\t{step.FrameCount},1,{step.VelocityX},{step.VelocityY}");
                    offset += 4;
                }

            }

            _writer.WriteCode(Code.Normal, "PathPtrs:");
            foreach (var path in _paths.Values.OrderBy(_ => _.Index))
            {
                _writer.WriteCode(Code.Normal, $"\tdc.l\tPaths+{path.Offset}\t; {path.Name}");
            }

        }

        private void WritePathComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for a path
** The structure is repeated until the FrameCount is 0. That is the end of the path. Enemy will disappear.
** Each path is formed by a number of these structure until framecount is 0.

PathFrameCount_b    equ     0   
PathMode_b          equ     1   ; 1 = Use velocity.
PathVX_b            equ     2   ; Velocity in 1//16th of pixel
PathVY_b            equ     3   
PATH_STRUCT_SIZE    equ     4
           
");
        }

        private void WriteWaves(LevelDefinition definition)
        {
            WriteWaveComments();
            _writer.WriteCode(Code.Normal, "Waves:");
            foreach (var wavePair in definition.Waves.OrderBy(_ => _.Value.Location))
            {
                var wave = wavePair.Value;
                _writer.WriteCode(Code.Normal, $"; wave '{wavePair.Key}'");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.Location}");

                var path = GetPathFor(wave.Path, wavePair.Key);
                var enemy = GetEnemyFor(wave.Enemy, wavePair.Key);

                _writer.WriteCode(Code.Normal, $"\t\tdc.b\t\t{wave.Count}, 0, {enemy.Index * 4}, {path.Index*4}\t; Path: {path.Name}, Enemy: {enemy.Name}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.Period}");

                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.StartX},{wave.StartY}");
                _writer.WriteCode(Code.Normal, $"\t\tdc.w\t\t{wave.StartXOffset},{wave.StartYOffset}");

            }
            _writer.WriteCode(Code.Normal, "; final wave past the end of the universe");
            _writer.WriteCode(Code.Normal, "\t\tdc.w\t\t$7fff\t\t");
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

WaveLocation_w      equ     0       ; In the X axis of the whole level
WaveEnemyCount_b    equ     2       
WaveUnused_b        equ     3
WaveEnemyOffset_b   equ     4       ; Offset in the Enemies label
WavePathOffset_b    equ     5       ; Offset in the PathPtrs label
WavePeriod_w        equ     6
WaveStartX_w        equ     8
WaveStartY_w        equ     10
WaveStartXOffset_w  equ     12
WaveStartYOffset_w  equ     14
WAVE_STRUCT_SIZE    equ     16


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
            _writer.WriteCode(Code.Normal, "**  @ MaskByteOffset interleaved planes with the data (same mask is repeated for each plane)");
            _writer.WriteCode(Code.Normal,"");
            _writer.WriteCode(Code.Normal, "BOBS_WIDTH\t\tequ\t0");
            _writer.WriteCode(Code.Normal, "BOBS_COUNT\t\tequ\t2");
            _writer.WriteCode(Code.Normal, "BOBS_STRUCT_SIZE\t\tequ\t4");
            _writer.WriteCode(Code.Normal, "BOB_PLANEOFFSET\t\tequ\t0");
            _writer.WriteCode(Code.Normal, "BOB_MASKOFFSET\t\tequ\t2");
            _writer.WriteCode(Code.Normal, "BOB_HEIGHT\t\tequ\t4");
            _writer.WriteCode(Code.Normal, "BOB_YADJUST\t\tequ\t6");
            _writer.WriteCode(Code.Normal, "BOB_STRUCT_SIZE\t\tequ\t8");

            _writer.WriteCode(Code.Normal, "");
        }

        private void ConvertBobPalette(ColorPalette palette, LevelDefinition definition)
        {
            _paletteRenderer.Render("bob", palette, definition.BobPlaneCount.PowerOfTwo());
        }
    }
}