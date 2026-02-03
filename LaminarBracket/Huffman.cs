using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class HuffmanNode
{
    public byte Symbol { get; set; }
    public int Frequency { get; set;  }
    public HuffmanNode Left { get; set; }
    public HuffmanNode Right { get; set; }

    public bool IsLeaf => Left == null && Right == null;
}

public class HuffmanCompressor : ICompressor
{
    public string Name => "Huffman";

    public byte[] Compress(byte[] data)
    {
        if (data == null || data.Length == 0) return new byte[0];

        var frequencies = new int[256];
        foreach (var b in data)
        {
            frequencies[b]++;
        }

        var root = BuildTree(frequencies);

        var codes = new string[256];
        GenerateCodes(root, "", codes);

        using (var ms = new MemoryStream())
        {
            var writer = new BinaryWriter(ms);
            var bitWriter = new BitWriter(ms);
            
            var activeSymbols = frequencies.Select((f, i) => new { Symbol = (byte)i, Count = f }).Where(x => x.Count > 0).ToList();
            writer.Write((int)activeSymbols.Count);
            foreach (var s in activeSymbols)
            {
                writer.Write(s.Symbol);
                writer.Write(s.Count);
            }
            
            writer.Write((int)data.Length);

            foreach (var b in data)
            {
                string code = codes[b];
                foreach (char c in code)
                {
                    bitWriter.WriteBit(c == '1' ? 1 : 0);
                }
            }
            
            bitWriter.Flush();
            return ms.ToArray();
        }
    }

    public byte[] Decompress(byte[] data)
    {
        if (data == null || data.Length == 0) return new byte[0];

        using (var input = new MemoryStream(data))
        using (var reader = new BinaryReader(input))
        {
            int numEntries = reader.ReadInt32();
            var frequencies = new int[256];
            for (int i = 0; i < numEntries; i++)
            {
                byte symbol = reader.ReadByte();
                int count = reader.ReadInt32();
                frequencies[symbol] = count;
            }

            int originalLength = reader.ReadInt32();

            var root = BuildTree(frequencies);
            
            var output = new byte[originalLength];
            var bitReader = new BitReader(input);
            var currentNode = root;
            
            for (int i = 0; i < originalLength; )
            {
                if (currentNode.IsLeaf)
                {
                    output[i++] = currentNode.Symbol;
                    currentNode = root; 
                    if (originalLength > 0 && root.IsLeaf) 
                    {
                         Array.Fill(output, root.Symbol);
                         for(int k=0; k<originalLength; k++) output[k] = root.Symbol;
                         break;
                    }
                    continue;
                }

                int bit = bitReader.ReadBit();
                if (bit == -1) break;

                if (bit == 0) currentNode = currentNode.Left;
                else currentNode = currentNode.Right;

                if (currentNode.IsLeaf)
                {
                    output[i++] = currentNode.Symbol;
                    currentNode = root;
                }
            }

            return output;
        }
    }

    private HuffmanNode BuildTree(int[] frequencies)
    {
        var priorityQueue = new PriorityQueue<HuffmanNode, int>();

        for (int i = 0; i < 256; i++)
        {
            if (frequencies[i] > 0)
            {
                priorityQueue.Enqueue(new HuffmanNode { Symbol = (byte)i, Frequency = frequencies[i] }, frequencies[i]);
            }
        }

        if (priorityQueue.Count == 0) return null;
        if (priorityQueue.Count == 1)
        {
             var node = priorityQueue.Dequeue();
             return node;
        }

        while (priorityQueue.Count > 1)
        {
            var left = priorityQueue.Dequeue();
            var right = priorityQueue.Dequeue();

            var parent = new HuffmanNode
            {
                Frequency = left.Frequency + right.Frequency,
                Left = left,
                Right = right
            };

            priorityQueue.Enqueue(parent, parent.Frequency);
        }

        return priorityQueue.Dequeue();
    }

    private void GenerateCodes(HuffmanNode node, string currentCode, string[] codes)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            codes[node.Symbol] = currentCode;
            if (currentCode == "") codes[node.Symbol] = "0"; 
            return;
        }

        GenerateCodes(node.Left, currentCode + "0", codes);
        GenerateCodes(node.Right, currentCode + "1", codes);
    }
}