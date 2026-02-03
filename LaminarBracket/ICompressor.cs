public interface ICompressor
{
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] data);
    string Name { get; }
}
