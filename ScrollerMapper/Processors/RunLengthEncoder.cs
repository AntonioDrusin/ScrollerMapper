using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrollerMapper.Processors
{
    internal static class RunLengthEncoder
    {
        private enum State
        {
            Repeat,
            Different,
            None,
        }

        public static void ProjectCompression(byte[] interleaved)
        {
            // negative : repeat
            // positive : copy pixels

            List<byte> compressed = new List<byte>();
            var pin = 0;
            var sameCount = 0;
            var diffCount = 0;
            var last = interleaved[0];
            var mode = State.None;

            for (int i = 1; i < interleaved.Length; i++)
            {
                var current = interleaved[i];
                if (last == current)
                {
                    sameCount++;
                    diffCount = 0;
                }
                else
                {
                    diffCount++;
                    sameCount = 0;
                }

                switch (mode)
                {
                    case State.None:
                        if (sameCount > 0) mode = State.Repeat;
                        else mode = State.Different;
                        break;
                    case State.Different:
                        if (i - pin == 127)
                        {
                            compressed.Add(127);
                            pin += 127;
                            mode = State.None;
                        }
                        else if (sameCount >= 3)
                        {
                            var len = i - pin - sameCount + 1;
                            compressed.Add((byte)(len));
                            compressed.AddRange(interleaved.Skip(pin).Take(len));
                            pin += len;
                            mode = State.Repeat;
                        }

                        break;
                    case State.Repeat:
                        if (i - pin == 128)
                        {
                            compressed.Add(128);
                            pin += 128;
                            mode = State.None;
                        }
                        if (diffCount > 0)
                        {
                            var len = i - pin;
                            compressed.Add((byte)-len);
                            compressed.Add(interleaved[i - 1]);
                            pin += len;
                            mode = State.Different;
                        }
                        break;
                }

            }

            Console.WriteLine("RLE projected compression " + interleaved.Length + "->" + compressed.Count);

        }
    }
}
