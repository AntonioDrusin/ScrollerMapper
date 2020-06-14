using System;
using System.Collections.Generic;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Transformers
{
    internal class SmoothInputPathTransformer
    {
        // returns an unoptimized list of step infos
        public IEnumerable<OutputPathStepInfo> TransformPath(IEnumerable<PathStepDefinition> steps)
        {
            PathStepDefinition previousStep = null; //new PathStepDefinition {X = 0, Y=-0};
            PathStepDefinition currentStep = null;

            foreach (var nextStep in steps.Concat(new List<PathStepDefinition>
                {new PathStepDefinition {X = 0, Y = 0, Mode = "_end"}}))
            {
                if (currentStep == null)
                {
                    currentStep = nextStep;
                    previousStep = new PathStepDefinition {X = 0, Y = 0};
                }
                else if (currentStep.Mode != "_end")
                {
                    AssertStep(currentStep);
                    for (int t = 0; t < currentStep.F; t++)
                    {
                        // Linear blending
                        var inMix = -(t - currentStep.In + 0.5) / currentStep.In;
                        var outMix = (t + 0.5 - (currentStep.F - currentStep.Out)) / currentStep.Out;
                        inMix = Math.Max(inMix, 0);
                        outMix = Math.Max(outMix, 0);
                        var curMix = 1 - inMix - outMix;

                        yield return new OutputPathStepInfo
                        {
                            FrameCount = 1,
                            VelocityX = DoubleToShort(
                                ((currentStep.X * curMix + previousStep.X * inMix + nextStep.X * outMix) /
                                 (inMix + outMix + curMix))),
                            VelocityY = DoubleToShort(
                                ((currentStep.Y * curMix + previousStep.Y * inMix + nextStep.Y * outMix) /
                                 (inMix + outMix + curMix)))
                        };
                    }

                    previousStep = currentStep;
                    currentStep = nextStep;
                }
            }
        }

        private short DoubleToShort(double value)
        {
            return (short)value;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void AssertStep(PathStepDefinition step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (step.X > 255)
            {
                throw new ConversionException("Velocity X cannot be larger than 255");
            }

            if (step.Y > 255)
            {
                throw new ConversionException("Velocity X cannot be larger than 255");
            }
        }
    }

    internal class OutputPathCoalesceTransformer
    {
        public IEnumerable<OutputPathStepInfo> GroupPath(IEnumerable<OutputPathStepInfo> steps)
        {
            short curVx = 0;
            short curVy = 0;
            long curFrame = 0;

            foreach (var step in steps.Concat(new[]
                {new OutputPathStepInfo {VelocityY = 0, VelocityX = 0, FrameCount = 0}}))
            {
                if (step.VelocityX != curVx || step.VelocityY != curVy)
                {
                    if (curFrame > 0)
                    {
                        while (curFrame > 0)
                        {
                            var emitCount = Math.Min(curFrame, 255);
                            yield return new OutputPathStepInfo
                            {
                                FrameCount = (byte) emitCount,
                                VelocityX = curVx,
                                VelocityY = curVy,
                            };
                            curFrame -= emitCount;
                        }
                    }

                    curFrame = step.FrameCount;
                    curVx = step.VelocityX;
                    curVy = step.VelocityY;
                }
                else
                {
                    curFrame += step.FrameCount;
                }
            }
        }
    }
}