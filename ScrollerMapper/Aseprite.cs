using System;
using System.Diagnostics;
using System.IO;

namespace ScrollerMapper
{
    internal static class Aseprite
    {
        private static readonly string AsepriteExe;

        static Aseprite()
        {
            AsepriteExe = Environment.GetEnvironmentVariable("ASEPRITE");
            if (!string.IsNullOrWhiteSpace(AsepriteExe))
            {
                return;
            }

            AsepriteExe = Environment.ExpandEnvironmentVariables("%ProgramW6432%\\Aseprite\\Aseprite.exe");
        }

        public static ConversionResult ConvertAnimation(string asepriteFile, string destinationFolder)
        {
            var combine = Path.Combine(destinationFolder, Path.GetFileName(asepriteFile));
            var jsonFile = Path.GetFullPath(Path.ChangeExtension(combine, ".json"));
            var pngFile = Path.GetFullPath(Path.ChangeExtension(combine, ".png"));
            asepriteFile = Path.GetFullPath(asepriteFile.FromInputFolder());

            var arguments =
                $"-b --data \"{jsonFile}\" --format json-array --sheet-type horizontal --sheet \"{pngFile}\" \"{asepriteFile}\"";
            var process = Process.Start(AsepriteExe, arguments);

            if (process == null)
            {
                throw new ConversionException(
                    $"Could not find '{AsepriteExe}' you can use the ASEPRITE env variable to point to it.");
            }

            if (process.WaitForExit(5000)) return new ConversionResult
            {
                BitmapFile = pngFile,
                JsonFile = jsonFile,
            };

        process.Kill();
            throw new ConversionException($"Could not run '{AsepriteExe}' with arguments '{arguments}'");
        }
    }

    internal class ConversionResult
    {
        public string BitmapFile;
        public string JsonFile;
    }
}

