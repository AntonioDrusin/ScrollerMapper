namespace ScrollerMapper
{
    internal enum Code
    {
        Chip,
        Normal
    }

    internal interface IWriter
    {
        void StartObject(ObjectType type, string name);
        void EndObject();
        void WriteByte(byte data);
        void WriteWord(ushort data);
        void WriteLong(uint data);
        void WriteBlob(byte[] data);
        void WriteCode(Code codeType, string code);
    }
}