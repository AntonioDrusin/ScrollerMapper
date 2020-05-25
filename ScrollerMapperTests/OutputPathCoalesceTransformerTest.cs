using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.Transformers;
using ScrollerMapperTests.Services;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class OutputPathCoalesceTransformerTest
    {
        private OutputPathCoalesceTransformer _transformer;

        [SetUp]
        public void SetUp()
        {
            _transformer = new OutputPathCoalesceTransformer();
        }

        [Test]
        public void GroupsTogether()
        {
            var input = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 0xf6, FrameCount = 1},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 0xf6, FrameCount = 4},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 0xf6, FrameCount = 1},
            };

            var result = _transformer.GroupPath(input);

            var expected = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 0xf6, FrameCount = 6},
            };
            CollectionAssert.AreEqual(expected.ToStringCollection(), result.ToStringCollection());
        }

        [Test]
        public void GroupsTogetherWhenAppropriate()
        {
            var input = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 1, FrameCount = 1},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 1, FrameCount = 4},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 2, FrameCount = 1},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 2, FrameCount = 4},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 2, FrameCount = 1},
            };

            var result = _transformer.GroupPath(input);

            var expected = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 1, FrameCount = 5},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 2, FrameCount = 6},
            };

            CollectionAssert.AreEqual(expected.ToStringCollection(), result.ToStringCollection());
        }

        [Test]
        public void SplitsLongerSequences()
        {
            var input = Enumerable.Range(0, 300).Select(_ => new OutputPathStepInfo { VelocityX = 1, VelocityY = 1, FrameCount = 1 }).ToList();

            var result = _transformer.GroupPath(input);

            var expected = new List<OutputPathStepInfo>
            {
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 1, FrameCount = 255},
                new OutputPathStepInfo {VelocityX = 1, VelocityY = 1, FrameCount = 45},
            };

            CollectionAssert.AreEqual(expected.ToStringCollection(), result.ToStringCollection());
        }

    }
}