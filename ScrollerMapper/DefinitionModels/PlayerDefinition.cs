using System.Collections.Generic;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.DefinitionModels
{
    internal class PlayerDefinition
    {
        public List<ShotsDefinition> Shots = new List<ShotsDefinition>();
        public DeathDefinition Death = new DeathDefinition();
        public int Vx = 32;
        public int Vy = 32;
    }

    internal class ShotsDefinition
    {
        public int Vx;
        public int MaxCount;
        public int Cooldown;
        public int Hit = 1;
        public string Sound;
        public BobDefinition Bob;
    }

    internal enum SpriteMode
    {
        ChipWithControlWords,
        Fast
    }

    internal class SpriteDefinition
    {
        public string File;
        public string Palette;
        public string SpriteNumber;
        public int StartX = 0;
        public int StartY = 0;
        public int Count = 1;
        public int Height = 16;
        public int TopTrim = 0;
        public int BottomTrim = 0;

        public SpriteMode Mode = SpriteMode.ChipWithControlWords;
    }
}

internal class DeathDefinition
{
    public int RayDuration = 100;
    public int SpawnDelay = 10;
    public int InvulnerabilityDuration = 100;
    public CoordinateDefinition Spawn = new CoordinateDefinition(160, 100);
    public int SpawnCelH = 2;
    public int SpawnCelV = 0;
    public PathDefinition Path = new PathDefinition();
}

internal class CoordinateDefinition
{
    public int X;
    public int Y;

    public CoordinateDefinition(int x, int y)
    {
        X = x;
        Y = y;
    }
}

