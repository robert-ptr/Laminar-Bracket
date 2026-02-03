
public class Hamming1611 // this also uses the position 0 bit, compared to Hamming1511 which simply ignores it
{
    private static Boolean MSB = false;
    public static void SetMSBMode()
    {
        MSB = true;
    }

    public static void SetLSBMode()
    {
        MSB = false;
    }

    public static int Encode(int dataBits)
    {
        int encoded15 = Hamming1511.Encode(dataBits);
        
        int globalParity = 0;
        for (int i = 0; i < 15; i++)
            globalParity ^= (encoded15 >> i) & 1;
        
        return encoded15 | (globalParity << 15);
    }

    public static int Decode(int receivedDataBits)
    {
        int errorPos = 0;
        for (int i = 1; i <= 15; i++)
            if (((receivedDataBits >> (i - 1)) & 1) == 1) errorPos ^= i;
        
        int totalParity = 0;
        for (int i = 0; i < 16; i++)
            totalParity ^= (receivedDataBits >> i) & 1;
        
        if (errorPos == 0 && totalParity == 0)
        {
            Console.Out.WriteLine("No error detected");
            return Hamming1511.Decode(receivedDataBits);
        }
        
        if (totalParity == 1)
        {
            receivedDataBits ^= (1 << (errorPos - 1));
            Console.Out.WriteLine("Single error detected and corrected");
            return Hamming1511.Decode(receivedDataBits);
        }
        
        Console.Out.WriteLine("Double error detected - Uncorrectable");
        return 0;
    }
}