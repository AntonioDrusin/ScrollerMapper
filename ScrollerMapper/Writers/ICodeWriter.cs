using System;
using System.Collections.Generic;

namespace ScrollerMapper.Writers
{
    internal interface ICodeWriter
    {
        void WriteNumericConstant(string nameBpl, int value);
        void WriteNumericConstant(string nameBpl, uint value);
        void IncludeBinary(CodeMemoryType memoryType, string binaryFile, string label);
        void AssemblyLongPointer(string value);
        void WriteStructValue<T>(string name, T structureValue);
        void WriteStructureDeclarationOfLongs(string name, string type, List<string> elements);
        void WriteStructureDeclaration<T>();
        void WriteIncludeComments(params string[] comments);
        void WriteArray<T>(string name, int grouping, IEnumerable<T> values);
        void WriteEnum<T>() where T : Enum;
    }
}