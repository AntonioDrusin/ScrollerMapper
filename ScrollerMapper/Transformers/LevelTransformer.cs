using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Transformers
{
    // Now I think it would be preferable to do data passes
    //
    // LevelDefinition -> Load bitmap, calculate numbers, render the bobs  -> Level
    // Level-> gets written out

    internal class LevelTransformer
    {
        public void SetLevel(LevelDefinition level)
        {
            var scoreBitmap = level.Panel.Scoreboard.ImageFile.FromInputFolder().LoadIndexedBitmap();
            LevelHeight = ScreenHeight - scoreBitmap.Height;
        }

        public int ScreenHeight = 256;
        public int LevelHeight { get; private set; }
    }
}
