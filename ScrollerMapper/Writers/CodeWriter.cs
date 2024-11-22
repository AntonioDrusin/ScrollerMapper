using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ScrollerMapper.Writers
{
    enum CodeMemoryType
    {
        Chip,
        Fast
    }

    internal class CodeWriter : ICodeWriter, IDisposable
    {
        private readonly Options _options;
        private readonly Lazy<IndentedTextWriter> _sourceDataFile;
        private readonly Lazy<IndentedTextWriter> _sourceIncludeFile;
        private IndentedTextWriter SourceDataFile => _sourceDataFile.Value;
        private IndentedTextWriter SourceIncludeFile => _sourceIncludeFile.Value;
        
        private HashSet<Type> _writtenTypes = new HashSet<Type>();
        private readonly HashSet<Tuple<string, string>> _writtenDynamicTypes =new HashSet<Tuple<string, string>>();
        private StringBuilder _includeTail = new StringBuilder();


        public CodeWriter(Options options)
        {
            _options = options;
            _sourceDataFile = CreateCodeFile("_data.asm");
            _sourceIncludeFile = CreateCodeFile("_data.i");

            SourceDataFile.WriteLine($"\tinclude {GetFileName("_data.i")}");
            SourceIncludeFile.WriteLine($"\tinclude <exec/types.i>");
        }

        public void WriteNumericConstant(string name, int value)
        {
            SourceIncludeFile.WriteLine($"{name}\t\tequ\t{value}");
        }
        public void WriteNumericConstant(string name, uint value)
        {
            SourceIncludeFile.WriteLine($"{name}\t\tequ\t{value}");
        }

        public void IncludeBinary(CodeMemoryType memoryType, string binaryFile, string label)
        {
            SourceDataFile.WriteLine();
            switch (memoryType)
            {
                case CodeMemoryType.Chip:
                    SourceDataFile.WriteLine("\tsection\t\t.MEMF_CHIP,data_c");
                    break;
                case CodeMemoryType.Fast:
                default:
                    SourceDataFile.WriteLine("\tsection\t\tdata");
                    break;
            }

            SourceDataFile.WriteLine($"{label}:");
            _includeTail.AppendLine($"\tXREF\t{label}");
            SourceDataFile.WriteLine($"\tincbin \"{AddIncludeFolder(binaryFile)}\"");
            SourceDataFile.WriteLine("\teven");
        }

        public void AssemblyLongPointer(string value)
        {
            SourceDataFile.WriteLine($"\tdc.l\t{value}");
        }

        public void WriteStructValue<T>(string name, T structureValue)
        {
            WriteStructureDeclaration<T>();
            SourceDataFile.WriteLine();
            SourceDataFile.WriteLine("\tsection data");
            SourceDataFile.WriteLine($"{name}:");
            _includeTail.AppendLine($"\tXREF\t{name}");
            foreach (var field in typeof(T).GetFields())
            {
                var assemblyField = new AssemblyCodeType(field.FieldType);
                var fieldDefinition = assemblyField.GetDefinition(field.GetValue(structureValue));
                SourceDataFile.WriteLine($"{fieldDefinition}\t;{field.Name}");
            }
        }

        public void WriteStructureDeclarationOfLongs(string name, string type, List<string> elements)
        {
            var key = Tuple.Create(name, type); // In case we create multiple level files
            if (!_writtenDynamicTypes.Add(key)) return;
            
            SourceIncludeFile.WriteLine();
            SourceIncludeFile.WriteLine("; Structure for file for {{name}} in {{type}} ram");
            SourceIncludeFile.WriteLine($"\tSTRUCTURE   {name}{type}Structure, 0");
            foreach (var element in elements)
            {
                SourceIncludeFile.WriteLine($"\tLONG\t\t{name}{type}{element}Ptr_l");
            }
            SourceIncludeFile.WriteLine($"\tLABEL       {name.ToUpper()}_{type.ToUpper()}_STRUCT_SIZE");
            SourceIncludeFile.WriteLine();
        }

        public void WriteStructureDeclaration<T>()
        {
            var type = typeof(T);
            if (!_writtenTypes.Add(type)) return;
            
            SourceIncludeFile.WriteLine();
            var attributes = type.GetCustomAttributes<CommentsAttribute>(false);
            foreach (var attr in attributes)
            {
                WriteIncludeComments(attr.Comment);
            }

            var initialOffset = "0";
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                initialOffset = GetStructureLabelName(type.BaseType.Name);
            }

            SourceIncludeFile.WriteLine($"\tSTRUCTURE\t{type.Name},{initialOffset}");
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                try
                {
                    var assemblyField = new AssemblyCodeType(field.FieldType);
                    var declaration = assemblyField.GetDeclaration(field);
                    var fieldComments = field.GetCustomAttributes<CommentsAttribute>().ToList();
                    if (fieldComments.Any())
                    {
                        if (fieldComments.Count == 1)
                        {
                            declaration += "\t ; " + fieldComments.Single().Comment.Replace("\n", " ");
                        }
                        else
                        {
                            foreach (var attr in fieldComments)
                            {
                                WriteIncludeComments(attr.Comment);
                            }
                        }
                    }

                    SourceIncludeFile.WriteLine(declaration);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while writing structure {type.Name} field {field.Name}: {ex.Message}");
                }
            }
            SourceIncludeFile.WriteLine($"\tLABEL\t{GetStructureLabelName(type.Name)}");
            SourceIncludeFile.WriteLine();
        }

        public void WriteIncludeComments(params string[] comments)
        {
            var writeComments = comments.SelectMany(line => line.Split('\n'));
            SourceIncludeFile.WriteLine();
            foreach (var comment in writeComments)
            {
                SourceIncludeFile.WriteLine($"; {comment}");
            }
        }

        public void WriteArray<T>(string name, int grouping, IEnumerable<T> values)
        {
            var at = new AssemblyCodeType(typeof(T));
            SourceDataFile.WriteLine();
            SourceDataFile.WriteLine("\tsection data");
            SourceDataFile.WriteLine($"{name}:");
            _includeTail.AppendLine($"\tXREF\t{name}");
            SourceDataFile.Write(at.GetArrayDefinition(grouping, values));
            SourceDataFile.WriteLine("\teven");
        }

        public void WriteEnum<T>() where T : Enum
        {
            throw new NotImplementedException();
        }

        private string GetStructureLabelName(string structureName)
        {
            var root = Regex.Replace(structureName, "Structure$", "");
            return $"{CamelCaseToUpperSnakeCase(root)}_STRUCT_SIZE";
        }
        
        private static string CamelCaseToUpperSnakeCase(string input)
        {
            return Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToUpper();
        }

        private Lazy<IndentedTextWriter> CreateCodeFile(string postFix)
        {
            var fileName = GetFileName(postFix);
            return new Lazy<IndentedTextWriter>(() =>
                new IndentedTextWriter(new StreamWriter(File.Create(fileName)), "\t"));
        }

        private string GetFileName(string postFix)
        {
            var fileName = Path.Combine(_options.OutputFolder,
                Path.GetFileNameWithoutExtension(_options.InputFile) + postFix);
            return fileName;
        }
        
        private string AddIncludeFolder(string fileName)
        {
            return Path.Combine(_options.OutputFolder, fileName);
        }

        public void Dispose()
        {
            SourceIncludeFile.WriteLine();
            SourceIncludeFile.Write(_includeTail);
            CloseFile(_sourceDataFile);
            CloseFile(_sourceIncludeFile);
        }

        private static void CloseFile(Lazy<IndentedTextWriter> writer)
        {
            if (!writer.IsValueCreated) return;
            writer.Value.Flush();
            writer.Value.Close();
            writer.Value.Dispose();
        }
    }
}