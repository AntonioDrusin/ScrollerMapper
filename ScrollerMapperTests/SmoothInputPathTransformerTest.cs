using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;
using ScrollerMapperTests.Services;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class SmoothInputPathTransformerTest
    {
        private SmoothInputPathTransformer _transformer;

        [SetUp]
        public void SetUp()
        {
            _transformer = new SmoothInputPathTransformer();
        }

        [Test]
        public void SplitsIntoFrames()
        {
            var input = new[]
            {
                new PathStepDefinition {F = 10, X = 4, Y = 4},
            };

            var result = _transformer.TransformPath(input);

            var expected = Enumerable.Range(0, 10)
                .Select(_ => new OutputPathStepInfo {FrameCount = 1, VelocityX = 4, VelocityY = 4});

            CollectionAssert.AreEqual( expected.ToStringCollection(), result.ToStringCollection());
        }

        [Test]
        public void LeavesJumpsAlone()
        {
            var input = new[]
            {
                new PathStepDefinition {F = 2, X = 4, Y = 4},
                new PathStepDefinition {F = 0, X = 0, Y = 0, Instruction = PathInstructionDefinition.Jump, Label = "Vel"},
                new PathStepDefinition {F = 2, X = 4, Y = 4},
            };

            var result = _transformer.TransformPath(input);

            var expected = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 4, VelocityY = 4 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 4, VelocityY = 4 },
                new OutputPathStepInfo { FrameCount = 0, VelocityX = 0, VelocityY = 0, Instruction = PathInstructionDefinition.Jump, Label = "Vel"},
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 4, VelocityY = 4 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 4, VelocityY = 4 }
            };

            CollectionAssert.AreEqual(expected.ToStringCollection(), result.ToStringCollection());
        }

        [Test]
        public void Smooths_Velocity()
        {
            var input = new[]
            {
                new PathStepDefinition {F = 5, X = 4, Y = -4, In=2, Out=2},   // fc == -4
                new PathStepDefinition {F = 1, X = 16, Y = 16},
            };

            var result = _transformer.TransformPath(input);

            var expected = new[]
            {
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 1, VelocityY = -1 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 3, VelocityY = -3 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 4, VelocityY = -4 },        // Correct speed
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 7, VelocityY = 1 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 13, VelocityY = 11 },
                new OutputPathStepInfo { FrameCount = 1, VelocityX = 16, VelocityY = 16 },
            };

            CollectionAssert.AreEqual(expected.ToStringCollection(), result.ToStringCollection());
        }
    }
}