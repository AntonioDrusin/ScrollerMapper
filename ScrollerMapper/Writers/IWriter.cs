namespace ScrollerMapper
{
    internal interface IWriter
    {
        void StartObject(ObjectType type, string name);
        void CompleteObject();
        void WriteByte(byte data);
        void WriteWord(ushort data);
        void WriteLong(uint data);
        void WriteBlob(byte[] data);
        void WriteCode(string code);
    }
}