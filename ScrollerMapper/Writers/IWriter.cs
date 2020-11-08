namespace ScrollerMapper
{
    internal enum Code
    {
        Data,
        Normal
    }

    internal interface IWriter
    {
        void StartDiskFile(string diskFileName);
        void CompleteDiskFile();
        void StartObject(ObjectType type, string name);
        void EndObject();
        void WriteByte(byte data);
        void WriteWord(ushort data);
        void WriteLong(uint data);
        void WriteBlob(byte[] data);
        void WriteBlob(byte[] data, int count);
        void WriteCode(Code codeType, string code);
        long GetCurrentOffset();
    }
}