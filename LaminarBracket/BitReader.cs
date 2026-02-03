public class BitReader
{
    private int buffer;
    private int bitsRead;
    private Stream s;
    
    public BitReader(Stream s)
    {
        this.s = s;
        this.buffer = s.ReadByte();
        this.bitsRead = 0;
    }

    public int ReadBit()
    {
        if (buffer == -1)
            return -1;

        int bit = (buffer >> (7 - bitsRead)) & 1;
        
        bitsRead++;

        if (bitsRead == 8)
        {
            this.buffer = s.ReadByte();
            bitsRead = 0;
        }

        return bit;
    }
}
