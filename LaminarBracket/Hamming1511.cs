
public class Hamming1511
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
    
    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
    
    public static int Encode(int dataBits)
    {
        int[] bits = new int[16];

        int dataPos = MSB ? 10 : 0;
        
        for (int i = 1; i <= 15; i++)
        {
            if (!IsPowerOfTwo(dataBits))
            {
                bits[i] = (dataBits >> dataPos) & 1;
                if (MSB)
                    dataPos--;
                else
                    dataPos++;
            }
        }

        for (int p = 0; p < 4; p++)
        {
            int pPos = (int)Math.Pow(2, p);
            int parity = 0;
            for (int i = 1; i <= 15; i++)
            {
                if ((i & pPos) != 0) parity ^= bits[i];
            }

            bits[pPos] = parity;
        }

        int result = 0;
        for (int i = 1; i <= 15; i++)
            if (bits[i] == 1) result |= (1 << (i - 1));
        
        return result;
    }

    public static int Decode(int dataBitsReceived)
    {
        int errorPos = 0; // for Hamming1511, if the errorPos is 0, it means there was either an error on the first bit, or no error
        // this does not matter since the first bit is unused

        for (int i = 1; i <= 15; i++)
        {
            if (((dataBitsReceived >> (i - 1)) & 1) == 1)
                errorPos ^= i;
        }

        if (errorPos != 0) // had an error
        {
            dataBitsReceived ^= (1 << (errorPos - 1)); // correct it
        }
      
        int data = 0;
        int[] dataPositions = { 3, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15 };
        foreach (int pos in dataPositions)
        {
            data <<= 1;
            data |= (dataBitsReceived >> (pos - 1)) & 1;
        }
        return data;
    }
}