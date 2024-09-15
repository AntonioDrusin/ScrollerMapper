using System;

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

            byte[] compressed = new byte[interleaved.Length];
            var pin = 0;
            var sameCount = 0;
            var diffCount = 0;
            var last = interleaved[0];
            var mode = State.None;
            var cix = 0;

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
                            compressed[cix++]=127;
                            pin += 127;
                            mode = State.None;
                        }
                        else if (sameCount >= 3)
                        {
                            var len = i - pin - sameCount + 1;
                            compressed[cix++]=(byte)len;
                            for (int t = 0; t < len; t++) compressed[cix++] = interleaved[pin++];
                            mode = State.Repeat;
                        }

                        break;
                    case State.Repeat:
                        if (i - pin == 128)
                        {
                            compressed[cix++] = 128;
                            pin += 128;
                            mode = State.None;
                        }
                        if (diffCount > 0)
                        {
                            var len = i - pin;
                            compressed[cix++]=(byte)-len;
                            compressed[cix++]=interleaved[i - 1];
                            pin += len;
                            mode = State.Different;
                        }
                        break;
                }

            }

            Console.WriteLine("RLE projected compression " + interleaved.Length + "->" + cix);

        }
    }
}
