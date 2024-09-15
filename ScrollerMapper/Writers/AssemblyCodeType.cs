using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScrollerMapper.Writers
{
    internal class AssemblyCodeType
    {
        private readonly string _type;
        private readonly string _suffix;
        private readonly string _formatString;

        public AssemblyCodeType(Type fieldType)
        {
            switch (fieldType.Name)
            {
                case "Byte":
                    _type = "BYTE";
                    _suffix = "b";
                    _formatString = "${0:X2}";
                    break;                    
                case "Int16": 
                case "UInt16":
                    _type = "WORD";
                    _suffix = "w";
                    _formatString = "${0:X4}";
                    break;
                case "Int32":
                case "UInt32":
                    _type = "LONG";
                    _suffix = "l";
                    _formatString = "${0:X8}";
                    break;
                // For now, we render string as long "pointers" and put the string as the label they are pointing to.
                case "String":
                    _type = "LONG";
                    _suffix = "l";
                    break;
                default:
                    throw new ArgumentException($"Invalid field type: {fieldType.Name}");
            }
        }
        public string GetDeclaration(FieldInfo field)
        {
            return $"\t{_type}\t{field.Name}_{_suffix}";
        }

        public string GetDefinition(object value)
        {
            return $"\tdc.{_suffix}\t{value}";
        }

        public string GetArrayDefinition<T>(int grouping, IEnumerable<T> values)
        {
            var sb = new StringBuilder();
            foreach (var chunk in values.Chunk(8))
            {
                sb.Append($"\tdc.{_suffix}\t");
                sb.AppendLine(string.Join(",", chunk.Select(c => String.Format(_formatString, c))));
            }
            return sb.ToString();
        }
    }
}