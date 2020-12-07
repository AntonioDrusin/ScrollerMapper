using System.Collections.Generic;
using System.ComponentModel;
using ScrollerMapper.Processors;

namespace ScrollerMapper.DefinitionModels
{
    internal class LevelDefinition
    {
        public Dictionary<string, TiledTileDefinition> Tiles { get; set; }
        public Dictionary<string, ImageDefinition> Images { get; set; }
        public Dictionary<string, BobDefinition> Bobs { get; set; }
        public Dictionary<string, EnemyDefinition> Enemies { get; set; }
        public Dictionary<string, WaveDefinition> Waves { get; set; }
        public Dictionary<string, PathDefinition> Paths { get; set; }
        public EnemyFireDefinition EnemyFire { get; set; }


        public DataDefinition Data { get; set; }
        
        public BackgroundDefinition Background { get; set; }
        public PanelDefinition Panel { get; set; }
        public int BobPlaneCount { get; set; }
        public string BobPaletteFile { get; set; }
        public bool BobPaletteFlip0AndLast { get; set; } = false;
        public string SpritePaletteFile { get; set; }
        public PlayerDefinition Player { get; set; }
        public int MaxActiveWaves { get; set; }
        public int MaxActiveEnemies { get; set; }
        public int MainVerticalBorder { get; set; } = 32;
        public int MainHorizontalBorder { get; set; } = 32;

        public SfxDefinition Sfx { get; set; }
        
        [DefaultValue(4)] public int FixedPointBits { get; set; } = 4;

        public LevelDetailsDefinition Level { get; set; }

        public void Validate()
        {
            if (Bobs == null)
            {
                throw new ConversionException("'bobs' must be defined.");
            }
            if (Level== null)
            {
                throw new ConversionException("'level' must be defined.");
            }
            if (Player == null)
            {
                throw new ConversionException("'player' must be defined.");
            }
            if (Waves == null)
            {
                throw new ConversionException("'waves' must be defined.");
            }
            if (Paths == null)
            {
                throw new ConversionException("'paths' must be defined.");
            }
            if (Enemies == null)
            {
                throw new ConversionException("'enemies' must be defined.");
            }
        }
    }

    internal class DataDefinition
    {
        public List<SpriteDefinition> Sprites { get; set; }
    }


    internal class BackgroundDefinition
    {
        public CopperShadeDefinition CopperShade { get; set; }
    }

    internal class CopperShadeDefinition
    {
        public ushort[] Colors { get; set; }
        public bool Flicker { get; set; } = false;
    }

    internal class LevelDetailsDefinition
    {
        public int Width { get; set; } = 1024;
    }
   
    internal class PanelDefinition
    {
        public ImageDefinition Font { get; set; }
        public ImageDefinition Scoreboard { get; set; }
        public MapDefinition Map { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    internal class MapDefinition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    internal class TiledTileDefinition
    {
        public string TiledFile { get; set; }
        public int PlaneCount { get; set; }
    }

    internal class ImageDefinition
    {
        public string ImageFile { get; set; }
        public int PlaneCount { get; set; }
    }

    internal class BobDefinition
    {
        public string ImageFile { get; set; }
        public int Width { get; set; }
        public int? Height { get; set; }
        public int? StartX { get; set; }
        public int? StartY { get; set; }
        public int? Count { get; set; }
    }

    internal class EnemyDefinition
    {
        public string Bob { get; set; }
        public int Points { get; set; }
        public ushort FrameDelay { get; set; }
        public ushort Hp { get; set; } = 2;
        public string ExplosionSound { get; set; } 
    }

    internal class WaveDefinition
    {
        public string Enemy { get; set; }
        public ushort OnExistingWaves { get; set; }
        public ushort FrameDelay { get; set; }
        public ushort Count { get; set; } // Enemy Count
        public ushort Period { get; set; }
        public short StartX { get; set; }
        public short StartY { get; set; }
        public short StartXOffset { get; set; }
        public short StartYOffset { get; set; }
        public string Path { get; set; }
        public string Fire { get; set; }
    }

    internal class PathDefinition
    {
        public string Mode { get; set; } = "v";
        public List<PathStepDefinition> Steps { get; set; } = new List<PathStepDefinition>();
    }

    internal class PathStepDefinition
    {
        public string Mode { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int F { get; set; }
        public int In { get; set; }
        public int Out { get; set; }
    }

    internal class SfxDefinition
    {
        public Dictionary<string, WaveformDefinition> Waveforms { get; set; } = new Dictionary<string, WaveformDefinition>();
        public Dictionary<string, SoundDefinition> Sounds { get; set; } = new Dictionary<string, SoundDefinition>();
    }

    internal class WaveformDefinition
    {
        public string SoundFile { get; set; }
    }

    internal class SoundDefinition
    {
        public string Waveform { get; set; }
        public int Frequency { get; set; } = 8000;
        public int Volume { get; set; } = 64;
    }

    internal class EnemyFireDefinition
    {
        public Dictionary<string, EnemyFireTypeDefinition> Types { get; set; } = new Dictionary<string, EnemyFireTypeDefinition>();
        public DirectFireDefinition Direct { get; set; } = new DirectFireDefinition();

    }

    internal class EnemyFireTypeDefinition
    {
        public string Sound { get; set; }
        public string Bob { get; set; }
        public EnemyFireMovements Movement { get; set; }
        public int Period { get; set; }
        public int Speed { get; set; }
    }

    internal class DirectFireDefinition
    {
        public int SlowSpeed { get; set; }
        public int NormalSpeed { get; set; }
        public double FastSpeed { get; set; }
    }
}