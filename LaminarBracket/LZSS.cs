using System.Runtime.InteropServices.Marshalling;

public struct Token
{
    public int Position;
    public int Length;
    public char C;

    public Token(int position, int length, char c)
    {
        this.Position = position;
        this.Length = length;
        this.C = c;
    }
    
    public override string ToString()
    {
        return $"({Position}, {Length}, '{C}')";
    }
};

public class LzssCompressor
{
    private const int MaxWindowSize = 4096;
    private const int MaxLookahead = 255;
    private BitWriter bw;
    private BitReader br;

    public LzssCompressor(Stream output)
    {
        bw = new BitWriter(output);
        br = new BitReader(output);
    }

    private void WriteByte(byte b)
    {
        for (int i = 7; i >= 0; i--)
        {
            bw.WriteBit((b >> i) & 1);
        }
    }
    
    private void WriteChar(char c) // writes a simple char
    {
        bw.WriteBit(0);
        WriteByte((byte)c);
    }

    private byte ReadByte()
    {
        byte buffer = 0;
        int bitsRead = 0;

        while (bitsRead < 8)
        {
            int bit =  br.ReadBit();

            if (bit == -1)
                break;
            
            buffer = (byte)((buffer << 1) | bit);
            bitsRead++;
        }

        return buffer;
    }

    private (int Start, int Length) FindLongestMatch(ReadOnlySpan<char> window, ReadOnlySpan<char> lookahead)
    {
        var bestLen = 0;
        var bestIndex = 0;
        var limit = Math.Min(lookahead.Length, MaxLookahead);
        
        for (int i = 0; i < window.Length; i++)
        {
            if (window[i] != lookahead[0])
                continue;

            var len = 1;
            
            while (len < limit && 
                   (i + len) < window.Length &&
                   window[i + len] == lookahead[len])
            {
                len++;
            }

            if (len > bestLen)
            {
                bestLen = len;
                bestIndex = i;

                if (bestLen == limit)
                    break;
            }
        }
        
        return (bestIndex, bestLen);
    }
    
    public void Compress(String message)
    {
        var tokens = new List<Token>();
        int position = 0;
        
        ReadOnlySpan<char> data = message.AsSpan();

        while (position < data.Length)
        {
            var windowStart = Math.Max(0, position - MaxWindowSize);
            var windowLength = position - windowStart;
            
            ReadOnlySpan<char> window = data.Slice(windowStart, windowLength);
            ReadOnlySpan<char> lookahead = data.Slice(position);
            
            var match = FindLongestMatch(window, lookahead);
            int d, l;
            char c;

            if (match.Length > 3)
            {
                d = window.Length - match.Start;
                l = match.Length;

                if (position + l < data.Length)
                {
                    c = data[position + l];
                }
                else
                {
                    c = '\0';
                }
            }
            else
            {
                d = 0;
                l = 0;
                c = data[position];
            }

            if (d != 0)
            {
                bw.WriteBit(1);
                WriteByte((byte)(d >> 8));
                WriteByte((byte)(d & 0xFF));
                WriteByte((byte)l);
                
                position += l;
            }
            else
            {
                WriteChar(c);

                position++;
            }
        }
        
        bw.Flush();
    }

    public String Decompress()
    {
        var sb = new System.Text.StringBuilder();

        while (true)
        {
            int bit = br.ReadBit();

            if (bit == -1)
                break;

            if (bit == 0)
            {
                sb.Append((char)ReadByte());
            }
            else
            {
                int d = ReadByte() << 8 | ReadByte();
                int l = ReadByte();
                
                int startIndex = sb.Length - d;
                    
                if (startIndex < 0) throw new Exception("Corrupt file: Invalid distance");
                
                for (int i = 0; i < l; i++)
                {
                    sb.Append(sb[startIndex + i]);
                }
            }
        }
        
        return sb.ToString();
    }
}
