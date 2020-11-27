namespace ScrollerMapper
{
    internal enum Code
    {
        Data,
        Normal
    }

    internal enum Destination
    {
        Executable,
        Disk
    }

    internal interface IWriter
    {
        void StartDiskFile(string diskFileName);
        void CompleteDiskFile();
        void StartObject(ObjectType type, string name);
        void RestartObject(ObjectType type, string name);
        void EndObject();
        void WriteByte(byte data);
        void WriteWord(ushort data);
        void WriteLong(uint data);
        void WriteBlob(byte[] data);
        void WriteBlob(byte[] data, int count);
        void WriteCode(Code codeType, string code);
        void WriteOffset(ObjectType objectType, uint offset);
        uint GetCurrentOffset(ObjectType objectType);
        uint GetOffset(string name);
    }
}