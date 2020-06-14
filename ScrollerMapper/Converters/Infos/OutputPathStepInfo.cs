
namespace ScrollerMapper.Converters.Infos
{
    internal class OutputPathStepInfo
    {
        public byte FrameCount { get; set; }
        public short VelocityX { get; set; }
        public short VelocityY { get; set; }

        public override string ToString()
        {
            return $"FrameCount: {FrameCount}, VelocityX: {VelocityX}, VelocityY: {VelocityY}";
        }
    }
}
