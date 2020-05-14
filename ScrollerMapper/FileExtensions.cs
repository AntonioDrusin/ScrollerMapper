using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ScrollerMapper
{
    internal static class FileExtensions
    {
        public static T ReadJsonFile<T>(this string fileName)
        {
            using (TextReader file = File.OpenText(fileName))
            {
                var reader = new JsonTextReader(file);
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public static T ReadXmlFile<T>(this string fileName) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var reader = XmlReader.Create(fileName))
            {
                return serializer.Deserialize(reader) as T;
            }
        }

        private static readonly List<PixelFormat> SupportedFormats = new List<PixelFormat>
        {
            PixelFormat.Format1bppIndexed, 
            PixelFormat.Format4bppIndexed, 
            PixelFormat.Format8bppIndexed
        };


        public static Bitmap LoadBitmap(this string fileName)
        {
            var bitmap = new Bitmap(fileName);
            if (!SupportedFormats.Contains(bitmap.PixelFormat))
            {
                throw new InvalidOperationException("Only indexed formats are supported");
            }

            return bitmap;
        }
    }
}