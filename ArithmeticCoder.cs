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
                value <<= 1;
                if (bit == 1) value += 1;
            }
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
