using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BurrowsWheeler : ICompressor
{
    public string Name => "BWT+MTF";

    private const int BlockSize = 4096;

    public byte[] Compress(byte[] data)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(data.Length);

            for (int i = 0; i < data.Length; i += BlockSize)
            {
                int len = Math.Min(BlockSize, data.Length - i);
                byte[] block = new byte[len];
                Array.Copy(data, i, block, 0, len);

                ProcessBlock(block, writer);
            }
            return ms.ToArray();
        }
    }

    public byte[] Decompress(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            int totalLength = reader.ReadInt32();
            byte[] result = new byte[totalLength];
            int resultPtr = 0;

            while (resultPtr < totalLength)
            {
                int primaryIndex = reader.ReadInt32();
                int remaining = totalLength - resultPtr;
                int currentBlockSize = Math.Min(BlockSize, remaining);
                
                byte[] block = reader.ReadBytes(currentBlockSize);
                
                byte[] decoded = InverseProcessBlock(block, primaryIndex);
                Array.Copy(decoded, 0, result, resultPtr, decoded.Length);
                resultPtr += decoded.Length;
            }
            return result;
        }
    }

    private void ProcessBlock(byte[] block, BinaryWriter writer)
    {
        int n = block.Length;
        int[] indices = new int[n];
        for (int k = 0; k < n; k++) indices[k] = k;
        
        Array.Sort(indices, (a, b) => 
        {
            for (int k = 0; k < n; k++)
            {
                byte valA = block[(a + k) % n];
                byte valB = block[(b + k) % n];
                if (valA != valB) return valA - valB;
            }
            return 0;
        });

        int primaryIndex = -1;
        byte[] L = new byte[n];
        
        for (int i = 0; i < n; i++)
        {
            if (indices[i] == 0) primaryIndex = i;
            L[i] = block[(indices[i] + n - 1) % n];
        }

        byte[] mtf = MoveToFront(L);

        writer.Write(primaryIndex);
        writer.Write(mtf);
    }

    private byte[] InverseProcessBlock(byte[] mtfBlock, int primaryIndex)
    {
        byte[] L = InverseMoveToFront(mtfBlock);
        
        int n = L.Length;
        int[] count = new int[256];
        foreach (var b in L) count[b]++;
        
        int[] accumulated = new int[256];
        int sum = 0;
        for (int i = 0; i < 256; i++)
        {
            accumulated[i] = sum;
            sum += count[i];
        }

        int[] T = new int[n];
        
        for (int i = 0; i < n; i++)
        {
            T[i] = accumulated[L[i]];
            accumulated[L[i]]++; 
        }

        byte[] original = new byte[n];
        int idx = primaryIndex;
        
        int current = primaryIndex;
        for (int i = n - 1; i >= 0; i--)
        {
            original[i] = L[current];
            current = T[current];
        }
        
        return original;
    }

    private byte[] MoveToFront(byte[] data)
    {
        byte[] result = new byte[data.Length];
        List<byte> list = new List<byte>(256);
        for (int i = 0; i < 256; i++) list.Add((byte)i);

        for (int i = 0; i < data.Length; i++)
        {
            byte c = data[i];
            int index = list.IndexOf(c);
            result[i] = (byte)index;
            
            list.RemoveAt(index);
            list.Insert(0, c);
        }
        return result;
    }

    private byte[] InverseMoveToFront(byte[] data)
    {
        byte[] result = new byte[data.Length];
        List<byte> list = new List<byte>(256);
        for (int i = 0; i < 256; i++) list.Add((byte)i);

        for (int i = 0; i < data.Length; i++)
        {
            int index = data[i];
            byte c = list[index];
            result[i] = c;
            
            list.RemoveAt(index);
            list.Insert(0, c);
        }
        return result;
    }
}