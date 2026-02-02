public class HuffmanNode
{
    public char Symbol { get; set; }
    public int Frequency { get; set;  }
    public HuffmanNode Left { get; set; }
    public HuffmanNode Right { get; set; }

    public bool IsLeaf => Left == null && Right == null;
}

public class HuffmanCodec
{
    public HuffmanNode BuildTree(string input)
    {
        var frequencies = input.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

        var priorityQueue = new PriorityQueue<HuffmanNode, int>();

        foreach (var kvp in frequencies)
        {
            priorityQueue.Enqueue(new HuffmanNode { Symbol = kvp.Key, Frequency = kvp.Value }, kvp.Value);
        }

        while (priorityQueue.Count > 1)
        {
            var left = priorityQueue.Dequeue();
            var right = priorityQueue.Dequeue();
            
            var parent = new HuffmanNode()
            {
                Symbol = '\0',
                Frequency = left.Frequency + right.Frequency,
                Left = left,
                Right = right
            };

            priorityQueue.Enqueue(parent, parent.Frequency);
        }

        return priorityQueue.Dequeue();
    }

    public void GenerateCodes(HuffmanNode node, string currentCode, Dictionary<char, string> codes)
    {
        if (node == null)
            return;

        if (node.IsLeaf)
        {
            codes[node.Symbol] = currentCode;
        }

        GenerateCodes(node.Left, currentCode + "0", codes);
        GenerateCodes(node.Right, currentCode + "1", codes);
    }

    public string Encode(string input, Dictionary<char, string> codes)
    {
        return string.Concat(input.Select(c => codes[c]));
    }
}