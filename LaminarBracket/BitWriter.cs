public class BitWriter
{
    private Stream s;
    private byte buffer = 0;
    private int bitsWritten = 0;
    
    public BitWriter(Stream s)
    {
        this.s = s;
    }

    public void WriteBit(int bit)
    {
        if (bit != 0 && bit != 1)
            throw new Exception("Bits must be 0 or 1.");

        buffer = (byte)((buffer << 1) | bit);
        
        bitsWritten++;
        
        if (bitsWritten == 8)
        {
            s.WriteByte(buffer);
            buffer = 0;
            bitsWritten = 0;
        }
    }

    public void Flush()
    {
        if (bitsWritten > 0)
        {
            buffer = (byte)(buffer << (8 - bitsWritten));
            s.WriteByte(buffer);

            buffer = 0;
            bitsWritten = 0;
        }
    }
}
