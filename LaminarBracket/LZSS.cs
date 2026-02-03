using System;
using System.Collections.Generic;
using System.IO;

public class LzssCompressor : ICompressor
{
    private readonly int maxWindowSize;
    private readonly int maxLookahead;
    private readonly bool lazyMatching;

    public string Name => $"LZSS (W={maxWindowSize}, L={maxLookahead}, Lazy={lazyMatching})";

    public LzssCompressor(int maxWindowSize = 4096, int maxLookahead = 255, bool lazyMatching = false)
    {
        this.maxWindowSize = maxWindowSize;
        this.maxLookahead = maxLookahead;
        this.lazyMatching = lazyMatching;
    }

    public byte[] Compress(byte[] data)
    {
        using (var ms = new MemoryStream())
        {
            var writer = new BinaryWriter(ms);
            writer.Write(data.Length);
            
            var bw = new BitWriter(ms);
            int position = 0;

            ReadOnlySpan<byte> span = data.AsSpan();

            while (position < span.Length)
            {
                var match = FindMatch(span, position);

                if (match.Length >= 3)
                {
                    bool useLiteral = false;

                    if (lazyMatching && position + 1 < span.Length)
                    {
                        var matchNext = FindMatch(span, position + 1);
                        
                        if (matchNext.Length > match.Length + 1)
                        {
                            useLiteral = true;
                        }
                    }

                    if (useLiteral)
                    {
                         bw.WriteBit(0); 
                         WriteByte(bw, span[position]);
                         position++;
                    }
                    else
                    {
                        int windowStart = Math.Max(0, position - maxWindowSize);
                        int windowLength = position - windowStart;
                        int distance = windowLength - match.Start;
                        int length = match.Length;

                        bw.WriteBit(1); 
                        WriteByte(bw, (byte)(distance >> 8));
                        WriteByte(bw, (byte)(distance & 0xFF));
                        WriteByte(bw, (byte)length);

                        position += length;
                    }
                }
                else
                {
                    bw.WriteBit(0); 
                    WriteByte(bw, span[position]);
                    position++;
                }
            }
            
            bw.Flush();
            return ms.ToArray();
        }
    }

    private (int Start, int Length) FindMatch(ReadOnlySpan<byte> data, int position)
    {
        var windowStart = Math.Max(0, position -maxWindowSize);
        var windowLength = position - windowStart;
        
        var window = data.Slice(windowStart, windowLength);
        var lookahead = data.Slice(position);
        
        return FindLongestMatch(window, lookahead);
    }

    public byte[] Decompress(byte[] data)
    {
        using (var input = new MemoryStream(data))
        using (var reader = new BinaryReader(input))
        using (var output = new MemoryStream())
        {
            int originalLength = reader.ReadInt32();
            var br = new BitReader(input);
            
            while (output.Position < originalLength)
            {
                int bit = br.ReadBit();
                if (bit == -1) break; 

                if (bit == 0) // Literal
                {
                    byte b = ReadByte(br);
                    output.WriteByte(b);
                }
                else // Pair
                {
                    int dHigh = ReadByte(br);
                    int dLow = ReadByte(br);
                    int distance = (dHigh << 8) | dLow;
                    int length = ReadByte(br);
                    
                    long currentPos = output.Position;
                    long copyStart = currentPos - distance;
                    
                    if (copyStart < 0) throw new Exception("Corrupt file: Invalid distance");

                    byte[] buffer = output.GetBuffer();
                    
                    for (int i = 0; i < length; i++)
                    {
                        output.WriteByte(buffer[copyStart + i]);
                    }
                }
            }
            return output.ToArray();
        }
    }

    private void WriteByte(BitWriter bw, byte b)
    {
        for (int i = 7; i >= 0; i--)
        {
            bw.WriteBit((b >> i) & 1);
        }
    }

    private byte ReadByte(BitReader br)
    {
        byte b = 0;
        for (int i = 0; i < 8; i++)
        {
            int bit = br.ReadBit();
            if (bit == -1) throw new EndOfStreamException();
            b = (byte)((b << 1) | bit);
        }
        return b;
    }

    private (int Start, int Length) FindLongestMatch(ReadOnlySpan<byte> window, ReadOnlySpan<byte> lookahead)
    {
        int bestLen = 0;
        int bestIndex = 0;
        int limit = Math.Min(lookahead.Length, maxLookahead);

        for (int i = 0; i < window.Length; i++)
        {
            if (window[i] != lookahead[0]) continue;
            
            int len = 1;
            while (len < limit && (i + len) < window.Length && window[i + len] == lookahead[len])
            {
                len++;
            }
            
            if (len > bestLen)
            {
                bestLen = len;
                bestIndex = i; 
                if (bestLen == limit) break;
            }
        }
        
        return (bestIndex, bestLen);
    }
}
