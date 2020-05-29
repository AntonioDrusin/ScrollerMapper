﻿using System.Collections.Generic;

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
        public ScoreDefinition Score { get; set; }
        public int BobPlaneCount { get; set; }
        public string BobPaletteFile { get; set; }
    }

    internal class ScoreDefinition
    {
        public ImageDefinition Font { get; set; }
        public ImageDefinition Scoreboard { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
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
        public int FrameDelay { get; set; }
        public int Width { get; set; }
    }

    internal class EnemyDefinition
    {
        public string Bob { get; set; }
        public int Points { get; set; }
        public int FrameDelay { get; set; }
    }

    internal class WaveDefinition
    {
        public string Enemy { get; set; }
        public int Location { get; set; }
        public int Count { get; set; } // Enemy Count
        public int Period { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartXOffset { get; set; }
        public int StartYOffset { get; set; }
        public string Path { get; set; }
    }

    internal class PathDefinition
    {
        public string Mode { get; set; }
        public List<PathStepDefinition> Steps { get; set; }
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
}