namespace ScrollerMapper.Converters.Infos
{
    internal enum OutputPathInstruction
    {
        Delta=0,
        End=1,
        Jump=2,
    }

    internal class OutputPathStepInfo
    {
        public byte FrameCount;
        public short VelocityX;
        public short VelocityY;
        public string Label;
        public OutputPathInstruction Instruction;

        public short JumpDelta
        {
            get => VelocityX;
            set => VelocityX = value;
        }

        public override string ToString()
        {
            return $"FrameCount: {FrameCount}, VelocityX: {VelocityX}, VelocityY: {VelocityY}, Label: '{Label}', Instruction: '{Instruction}', JumpDelta: {JumpDelta}";
        }
    }
}