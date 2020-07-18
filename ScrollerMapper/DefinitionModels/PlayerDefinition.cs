namespace ScrollerMapper.DefinitionModels
{
    internal class PlayerDefinition
    {
        public SpriteDefinition MainSprite { get; set; }
        public SpriteDefinition GrazingSprite { get; set; }
        public ShotsDefinition Shots { get; set; }
        public DeathDefinition Death { get; set; } = new DeathDefinition();
        public int? Vx { get; set; }
        public int? Vy { get; set; }
    }

    internal class ShotsDefinition
    {
        public int Vx { get; set; }
        public int MaxCount { get; set; }
        public int Cooldown { get; set; }
        public BobDefinition Bob { get; set; }
    }

    internal class SpriteDefinition
    {
        public string File { get; set; }
        public string Palette { get; set; }
        public string SpriteNumber { get; set; }
        public int StartX { get; set; } = 0;
        public int StartY { get; set; } = 0;
        public int Count { get; set; } = 1;
        public int Height { get; set; } = 16;
        public int Duration { get; set; } = 500; // In milliseconds
    }

    internal class DeathDefinition
    {
        public int RayDuration { get; set; } = 100;
        public int SpawnDelay { get; set; } = 10;
        public int InvulnerabilityDuration { get; set; } = 100;
        public CoordinateDefinition Spawn { get; set; } = new CoordinateDefinition(160, 100);
        public int SpawnCelH { get; set; } = 2;
        public int SpawnCelV { get; set; } = 0;
        public PathDefinition Path { get; set; } = new PathDefinition();
    }

    internal class CoordinateDefinition
    {
        public int X { get; set; }
        public int Y { get; set; }

        public CoordinateDefinition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

}