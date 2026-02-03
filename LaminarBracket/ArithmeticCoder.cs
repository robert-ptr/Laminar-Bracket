using System;
using System.IO;

public struct Probability
{
    public long Low;
    public long High;
    public long Count;
}

public interface IModel
{
    long MaxCode { get; }
    long OneHalf { get; }
    long OneFourth { get; }
    long ThreeFourths { get; }
    int CodeValueBits { get; }
    long Count { get; }

    Probability GetProbability(int symbol);
    Probability GetChar(long scaledValue, out int decodedSymbol);
    void Update(int symbol);
}

public class ArithmeticCompressor : ICompressor
{
    public string Name => "Arithmetic";

    public byte[] Compress(byte[] data)
    {
        using (var ms = new MemoryStream())
        using (var input = new MemoryStream(data))
        {
            var model = new AdaptiveModel();
            ArithmeticCodingEngine.Compress(input, ms, model);
            return ms.ToArray();
        }
    }

    public byte[] Decompress(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var output = new MemoryStream())
        {
            var model = new AdaptiveModel();
            try 
            {
                ArithmeticCodingEngine.Decompress(ms, output, model);
            }
            catch (EndOfStreamException) 
            {
            }
            return output.ToArray();
        }
    }
}

public class AdaptiveModel : IModel
{
    private const int MaxFrequency = 16383;

    private int[] frequencies;
    private int[] cumulative;
    private int total;
    private bool frozen = false;

    public const int CodeValueBits = 30;
    public long MaxCode => (1L << CodeValueBits) - 1;
    public long OneHalf => 1L << (CodeValueBits - 1);
    public long OneFourth => 1L << (CodeValueBits - 2);
    public long ThreeFourths => 3L * OneFourth;
    int IModel.CodeValueBits => CodeValueBits;
    public long Count => total;

    public AdaptiveModel()
    {
        frequencies = new int[258];
        cumulative = new int[259];
        
        for (int i = 0; i <= 256; i++) frequencies[i] = 1;
        UpdateCumulative();
    }

    private void UpdateCumulative()
    {
        int sum = 0;
        for (int i = 0; i <= 256; i++)
        {
            cumulative[i] = sum;
            sum += frequencies[i];
        }
        cumulative[257] = sum;
        total = sum;
    }

    public Probability GetProbability(int symbol)
    {
        return new Probability
        {
            Low = cumulative[symbol],
            High = cumulative[symbol + 1],
            Count = total
        };
    }

    public Probability GetChar(long scaledValue, out int decodedSymbol)
    {
        int left = 0;
        int right = 256;
        int found = 0;
        
        while (left <= right)
        {
            int mid = (left + right) / 2;
            if (scaledValue >= cumulative[mid] && scaledValue < cumulative[mid + 1])
            {
                found = mid;
                break;
            }
            else if (scaledValue < cumulative[mid])
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }
        
        decodedSymbol = found;
        return GetProbability(found);
    }

    public void Update(int symbol)
    {
        frequencies[symbol]++;
        if (total >= MaxFrequency)
        {
            for (int i = 0; i <= 256; i++)
            {
                frequencies[i] = (frequencies[i] + 1) / 2;
            }
        }
        UpdateCumulative();
    }
}

public static class ArithmeticCodingEngine
{
    private const int EOF_SYMBOL = 256;

    public static void Compress<TModel>(Stream input, Stream output, TModel model) where TModel : IModel
    {
        BitWriter writer = new BitWriter(output);
        int pendingBits = 0;
        long low = 0;
        long high = model.MaxCode;

        while (true)
        {
            int c = input.ReadByte();
            if (c == -1) c = EOF_SYMBOL;

            var p = model.GetProbability(c);
            long range = high - low + 1;
            high = low + (range * p.High / p.Count) - 1;
            low = low + (range * p.Low / p.Count);

            while (true)
            {
                if (high < model.OneHalf)
                {
                    PutBitPlusPending(0, ref pendingBits, writer);
                }
                else if (low >= model.OneHalf)
                {
                    PutBitPlusPending(1, ref pendingBits, writer);
                }
                else if (low >= model.OneFourth && high < model.ThreeFourths)
                {
                    pendingBits++;
                    low -= model.OneFourth;
                    high -= model.OneFourth;
                }
                else
                {
                    break;
                }

                high <<= 1;
                high++;
                low <<= 1;
                high &= model.MaxCode;
                low &= model.MaxCode;
            }
            
            model.Update(c);

            if (c == EOF_SYMBOL) break;
        }

        pendingBits++;
        if (low < model.OneFourth)
            PutBitPlusPending(0, ref pendingBits, writer);
        else
            PutBitPlusPending(1, ref pendingBits, writer);

        writer.Flush();
    }

    public static void Decompress<TModel>(Stream input, Stream output, TModel model) where TModel : IModel
    {
        BitReader reader = new BitReader(input);
        long high = model.MaxCode;
        long low = 0;
        long value = 0;

        for (int i = 0; i < model.CodeValueBits; i++)
        {
            int bit = reader.ReadBit();
            value <<= 1;
            if (bit == 1) value += 1;
            if (bit == -1) bit = 0;
        }

        while (true)
        {
            long range = high - low + 1;
            long scaledValue = ((value - low + 1) * model.Count - 1) / range;
            int c;
            var p = model.GetChar(scaledValue, out c);

            if (c == EOF_SYMBOL) break;

            output.WriteByte((byte)c);

            high = low + (range * p.High / p.Count) - 1;
            low = low + (range * p.Low / p.Count);

            while (true)
            {
                if (high < model.OneHalf) { }
                else if (low >= model.OneHalf)
                {
                    value -= model.OneHalf;
                    low -= model.OneHalf;
                    high -= model.OneHalf;
                }
                else if (low >= model.OneFourth && high < model.ThreeFourths)
                {
                    value -= model.OneFourth;
                    low -= model.OneFourth;
                    high -= model.OneFourth;
                }
                else
                {
                    break;
                }

                low <<= 1;
                high <<= 1;
                high++;
                
                int bit = reader.ReadBit();
                if (bit == -1) bit = 0;
                
                value <<= 1;
                if (bit == 1) value += 1;
            }
            
            model.Update(c);
        }
    }

    private static void PutBitPlusPending(int bit, ref int pendingBits, BitWriter writer)
    {
        writer.WriteBit(bit);
        int oppositeBit = (bit == 0) ? 1 : 0;
        for (int i = 0; i < pendingBits; i++) writer.WriteBit(oppositeBit);
        pendingBits = 0;
    }
}
