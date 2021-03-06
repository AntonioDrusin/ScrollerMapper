﻿using System.Collections.Generic;
using System.ComponentModel;
using ScrollerMapper.Processors;

namespace ScrollerMapper.DefinitionModels
{
    internal class LevelDefinition
    {
        public Dictionary<string, TiledTileDefinition> Tiles;
        public Dictionary<string, ImageDefinition> Images;
        public Dictionary<string, BobDefinition> Bobs;
        public Dictionary<string, EnemyDefinition> Enemies;
        public Dictionary<string, WaveDefinition> Waves;
        public Dictionary<string, PathDefinition> Paths;
        public EnemyFireDefinition EnemyFire;
        public List<BonusDefinition> Bonuses = new List<BonusDefinition>();
        
        public DataDefinition Data;
        
        public BackgroundDefinition Background;
        public PanelDefinition Panel;
        public int BobPlaneCount;
        public string BobPaletteFile;
        public bool BobPaletteFlip0AndLast = false;
        public string SpritePaletteFile;
        public PlayerDefinition Player;
        public int MaxActiveWaves;
        public int MaxActiveEnemies;
        public int MainVerticalBorder = 32;
        public int MainHorizontalBorder = 32;

        public SfxDefinition Sfx;

        [DefaultValue(4)] public int FixedPointBits = 4;

        public LevelDetailsDefinition Level;

        public void Validate()
        {
            if (Bobs == null)
            {
                throw new ConversionException("'bobs' must be defined.");
            }

            if (Level == null)
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
    }


    internal class BackgroundDefinition
    {
        public CopperShadeDefinition CopperShade;
    }

    internal class CopperShadeDefinition
    {
        public ushort[] Colors;
        public bool Flicker = false;
    }

    internal class LevelDetailsDefinition
    {
        public int Width = 1024;
    }

    internal class PanelDefinition
    {
        public ImageDefinition Font;
        public ImageDefinition Scoreboard;
        public MapDefinition Map;
        public int X;
        public int Y;
    }

    internal class MapDefinition
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }

    internal class TiledTileDefinition
    {
        public string TiledFile;
        public int PlaneCount;
    }

    internal class ImageDefinition
    {
        public string ImageFile;
        public int PlaneCount;
    }

    internal class BobDefinition
    {
        public string ImageFile;
        public int Width;
        public int? Height;
        public int StartX = 0;
        public int StartY = 0;
        public int? Count;
    }

    internal class EnemyDefinition
    {
        public string Bob;
        public int Points;
        public ushort FrameDelay;
        public ushort Hp = 2;
        public string ExplosionSound;
        public string PortalBob;
    }

    internal class WaveDefinition
    {
        public string Enemy;
        public ushort OnExistingWaves = 100;
        public ushort FrameDelay;
        public ushort Count; // Enemy Count
        public ushort Period;
        public short StartX;
        public short StartY;
        public short StartXOffset;
        public short StartYOffset;
        public string Path;
        public string Fire;
        public byte[] Bonus = new byte[] {0, 0, 0, 0};
        public ushort ExtraBonus = 0;
    }

    internal enum PathModeDefinition
    {
        Delta,
        CenterToCircle
    }

    internal class PathDefinition
    {
        public PathModeDefinition Mode = PathModeDefinition.Delta;
        public List<PathStepDefinition> Steps = new List<PathStepDefinition>();
        // For CenterToCircle path
        public int CenterX = 160;
        public int CenterY = 100;
        public int Diameter = 120;
        public int StartX = 160;
        public int StartY = 100;
        public double Speed = 1.0;
    }

    internal enum PathInstructionDefinition
    {
        Delta=0,
        End=1,
        Jump=2,
        HomeOnTarget=3,
    }

    internal class PathStepDefinition
    {
        public PathInstructionDefinition Instruction = PathInstructionDefinition.Delta;
        public int X;
        public int Y;

        public int V1
        {
            get => X;
            set => X = value;
        }
        public int V2
        {
            get => Y;
            set => Y = value;
        }

        public int F;
        public int In;
        public int Out;
        public string Label;
    }

    internal class SfxDefinition
    {
        public Dictionary<string, WaveformDefinition> Waveforms = new Dictionary<string, WaveformDefinition>();
        public Dictionary<string, SoundDefinition> Sounds = new Dictionary<string, SoundDefinition>();
    }

    internal class WaveformDefinition
    {
        public string SoundFile;
    }

    internal class SoundDefinition
    {
        public string Waveform;
        public int Frequency = 8000;
        public int Volume = 64;
    }

    internal class EnemyFireDefinition
    {
        public Dictionary<string, EnemyFireTypeDefinition> Types = new Dictionary<string, EnemyFireTypeDefinition>();
        public DirectFireDefinition Direct = new DirectFireDefinition();
    }

    internal class EnemyFireTypeDefinition
    {
        public string Sound;
        public string Bob;
        public EnemyFireMovements Movement;
        public int Period;
        public int Speed;
    }

    internal class DirectFireDefinition
    {
        public int SlowSpeed;
        public int NormalSpeed;
        public double FastSpeed;
    }

    internal class BonusDefinition
    {
        public string Bob;
    }
}