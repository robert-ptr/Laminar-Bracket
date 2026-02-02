public class Program
{
    public static void Main()
    {
        /*
        string text = "aaaaaabbccddeefffff";
        var codec = new HuffmanCodec();
        
        var root = codec.BuildTree(text);
        var codes = new Dictionary<char, string>();
        codec.GenerateCodes(root, "", codes);

        Console.WriteLine("Huffman Codes:");
        foreach (var kvp in codes) Console.WriteLine($"{kvp.Key}: {kvp.Value}");

        string encoded = codec.Encode(text, codes);
        Console.WriteLine($"\nEncoded bitstream: {encoded}");
        Console.WriteLine($"Original size: {text.Length * 8} bits");
        Console.WriteLine($"Compressed size: {encoded.Length} bits");
        */
        
        var bwt = new BurrowsWheeler();
        string original = "BANANA";
        
        string transformed = bwt.Transform(original);
        Console.WriteLine($"Original: {original}");
        Console.WriteLine($"BWT: {transformed}");
        
        string inverted = bwt.InverseTransform(transformed);
        Console.WriteLine($"Inverted: {inverted}");
    }
}