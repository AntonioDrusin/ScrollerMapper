
namespace ScrollerMapper.Converters.Infos
{
    internal class OutputPathStepInfo
    {
        public byte FrameCount { get; set; }
        public byte VelocityX { get; set; }
        public byte VelocityY { get; set; }

        public override string ToString()
        {
            return $"FrameCount: {FrameCount}, VelocityX: {VelocityX}, VelocityY: {VelocityY}";
        }
    }
}
