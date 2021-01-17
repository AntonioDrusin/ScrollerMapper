using System;
using System.Collections.Generic;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Processors;

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
                {new PathStepDefinition {X = 0, Y = 0, Instruction = PathInstruction.End}}))
            {
                if (currentStep == null)
                {
                    currentStep = nextStep;
                    previousStep = new PathStepDefinition {X = 0, Y = 0};
                }
                else 
                {
                    if (currentStep.Instruction == PathInstruction.Delta)
                    {
                        var labelEmitted = string.IsNullOrWhiteSpace(currentStep.Label);
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
                                     (inMix + outMix + curMix))),
                                Label = labelEmitted ? null : currentStep.Label,
                            };
                            labelEmitted = true;
                        }
                    }
                    else
                    {
                        yield return new OutputPathStepInfo
                        {
                            Label = currentStep.Label,
                            Instruction = MapInstruction(currentStep.Instruction),
                        };
                    }

                    previousStep = currentStep;
                    currentStep = nextStep;
                }
            }
        }

        private OutputPathInstruction MapInstruction(PathInstruction instruction)
        {
            switch (instruction)
            {
                case PathInstruction.Delta:
                    return OutputPathInstruction.Delta;
                case PathInstruction.Jump:
                    return OutputPathInstruction.Jump;
                case PathInstruction.End:
                    return OutputPathInstruction.End;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instruction), instruction, null);
            }
        }

        private short DoubleToShort(double value)
        {
            return (short) value;
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
        public IEnumerable<OutputPathStepInfo> ProcessPath(IEnumerable<OutputPathStepInfo> steps)
        {
            short curVx = 0;
            short curVy = 0;
            long curFrame = 0;
            OutputPathInstruction curInstruction = OutputPathInstruction.Delta;
            string emitLabel = null;
            var output = new List<OutputPathStepInfo>();

            var endCode = new OutputPathStepInfo {VelocityY = 0, VelocityX = 0, FrameCount = 0, Instruction = OutputPathInstruction.End};
            foreach (var step in steps.Concat(new[]
                {
                    endCode
                }))
            {
                if (step.VelocityX != curVx || step.VelocityY != curVy || !string.IsNullOrWhiteSpace(step.Label) || step.Instruction != curInstruction)
                {
                    if (curInstruction == OutputPathInstruction.Delta)
                    {
                        if (curFrame > 0)
                        {
                            while (curFrame > 0)
                            {
                                var emitCount = Math.Min(curFrame, 255);
                                output.Add(
                                 new OutputPathStepInfo
                                 {
                                     FrameCount = (byte)emitCount,
                                     VelocityX = curVx,
                                     VelocityY = curVy,
                                     Label = emitLabel,
                                     Instruction = curInstruction,
                                 });
                                emitLabel = null;
                                curFrame -= emitCount;
                            }
                        }
                    }
                    else
                    {
                        output.Add(
                         new OutputPathStepInfo
                         {
                             FrameCount = 0,
                             VelocityX = 0,
                             VelocityY = 0,
                             Label = emitLabel,
                             Instruction = curInstruction,
                         });
                    }
                    
                    curFrame = step.FrameCount;
                    curVx = step.VelocityX;
                    curVy = step.VelocityY;
                    curInstruction = step.Instruction;
                    emitLabel = step.Label;
                }
                else
                {
                    curFrame += step.FrameCount;
                }
            }

            // Get list of labels 
            var offset = 0;
            var labels = new Dictionary<string, int>();
            foreach (var step in output)
            {
                if (step.Instruction != OutputPathInstruction.Jump &&
                    !string.IsNullOrWhiteSpace(step.Label))
                {
                    labels.Add(step.Label, offset);
                }
                offset += PathsProcessor.PathStructSize;
            }

            // Process jump instructions
            offset = 0;
            foreach (var step in output)
            {
                try
                {
                    if (step.Instruction == OutputPathInstruction.Jump)
                    {
                        var jumpOffset = labels[step.Label] - offset;
                        step.FrameCount = 0;
                        step.VelocityX = 0;
                        step.VelocityY = 0;
                        step.Label = null;
                        step.JumpDelta = (short)jumpOffset;
                    }
                }
                catch
                {
                    throw new ConversionException($"Cannot find label {step.Label} for path jump");
                }

                offset += PathsProcessor.PathStructSize;
            }

            output.Add(endCode);
            return output;
        }

    }
}