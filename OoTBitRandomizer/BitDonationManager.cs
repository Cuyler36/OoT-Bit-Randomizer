using System;
using System.Linq;
using System.Threading;

namespace OoTBitRaceRandomizer
{
    public static class BitDonationManager
    {
        private static readonly Random Generator = new Random();
        private static readonly CodeInfo[] CodeInfoArray =
        {
            new CodeInfo("Time Modifier",           "time",         0x11A5DC,   1,   (ushort)0,     (ushort)0xFFFF), // 0xC000 is the start of night 0x0000 = Midnight? 0x4500 = start of day
            new CodeInfo("Tunic Color",             "tunic",        0x0F7AD8,   1,     (byte)0,         (byte)0xFF),
            new CodeInfo("Ocarina Sound",           "ocarina",      0x10220C,   1,     (byte)1,            (byte)8),
            new CodeInfo("Current Arrows",          "arrows",       0x11A65F,   5,     (byte)0,           (byte)51),
            new CodeInfo("Current Bombs",           "bombs",        0x11A65E,   5,     (byte)0,           (byte)41),
            new CodeInfo("Current Bombchus",        "bombchus",     0x11A664,   5,     (byte)0,           (byte)51),
            new CodeInfo("Current Slingshot Ammo",  "slingshot",    0x11A662,   5,     (byte)0,           (byte)31),
            new CodeInfo("Current Deku Nuts",       "nuts",         0x11A65D,   5,     (byte)0,           (byte)41),
            new CodeInfo("Current Deku Sticks",     "sticks",       0x11A65C,   5,     (byte)0,           (byte)31),
            new CodeInfo("Current Magic",           "magic",        0x11A603,   5,     (byte)0,           (byte)49),
            new CodeInfo("Current Rupees",          "rupees",       0x11A604,   5,   (ushort)0,     (ushort)0x01F5),
            new CodeInfo("Current Health",          "health",       0x11A600,   5,   (ushort)0,     (ushort)0x0201),
            new CodeInfo("Current B Item",          "equipped",     0x11A638,   10,    (byte)1,         (byte)0x3E),
            //new CodeInfo("Chang Equipped Sword",    0x13F54D,   5,   (byte)0x3B,         (byte)0x3E), // 3B = Kokiri Sword, 3D = Biggorn Sword
            // Link Position: floats? 0x13F434, 0x13F438, 0x13F43C (X, Y, Z?)
            // 0x11A64C = Item Bitmap

        };

        /// <summary>
        /// Returns a Randomized value between Min and Max, with the Minimum adjusted by the bit amount donated.
        /// </summary>
        /// <param name="Bits">How many bits were donated</param>
        /// <param name="Min">The minimum value to be returned</param>
        /// <param name="Max">The maximum value to be returned</param>
        /// <returns>Random number between min and max, with the min adjusted by the bit amount donated.</returns>
        public static T BitsToModifierPower<T>(int Bits, T Min, T Max)
        {
            Type TType = Min.GetType();
            if (TType == Max.GetType())
            {
                try
                {
                    dynamic dMin = (dynamic)Min;
                    dynamic dMax = (dynamic)Max;
                    float BitPercentage = 1 / dMax;
                    dynamic AdjustedMinimum = (dynamic)Math.Min(dMax, Math.Max(dMin, (dynamic)Convert.ChangeType(BitPercentage * Bits, TType)));
                    return (T)Convert.ChangeType(Generator.Next(AdjustedMinimum, dMax), TType);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            return Min;
        }

        /// <summary>
        /// Returns a random CodeInfo object that is valid for the bit donation amount supplied.
        /// </summary>
        /// <param name="BitAmount">The bit donation amount</param>
        /// <returns>A random CodeInfo whose required bit donation amount is less-than or equal-to the bit donation amount.</returns>
        public static CodeInfo GetRandomCodeInfoForBitAmount(int BitAmount)
        {
            var ValidCodeInfos = CodeInfoArray.Where(c => c.MinimumBitDonation <= BitAmount);
            int InfoCount = ValidCodeInfos.Count();

            if (InfoCount < 1)
            {
                return null;
            }

            return ValidCodeInfos.ElementAt(Generator.Next(0, InfoCount));
        }

        /// <summary>
        /// Returns a code that matches a command string, or if no matches were found, a random code.
        /// </summary>
        /// <param name="Input">The command string</param>
        /// <param name="BitAmount">The amount of bits donated</param>
        /// <returns>A tuple containing the selected CodeInfo (possibly null if 0 bits were donated somehow), and a success code.</returns>
        public static Tuple<CodeInfo, int> GetRequestedCode(string Input, int BitAmount)
        {
            Input = Input.ToLower();

            for (int i = 0; i < CodeInfoArray.Length; i++)
            {
                if (CodeInfoArray[i].CommandName.Equals(Input))
                {
                    if (CodeInfoArray[i].MinimumBitDonation <= BitAmount)
                    {
                        return new Tuple<CodeInfo, int>(CodeInfoArray[i], 1);
                    }
                    else
                    {
                        return new Tuple<CodeInfo, int>(GetRandomCodeInfoForBitAmount(BitAmount), -1);
                    }
                }
            }

            return new Tuple<CodeInfo, int>(GetRandomCodeInfoForBitAmount(BitAmount), 0);
        }

        /// <summary>
        /// Returns the bits required for a code.
        /// </summary>
        /// <param name="Name">The command name of the code.</param>
        /// <returns>The minimum bit donation required to run the code. If the code doesn't exist, -1 is returned.</returns>
        public static int GetBitsRequiredForCodeByCommandName(string Name)
        {
            for (int i = 0; i < CodeInfoArray.Length; i++)
            {
                if (CodeInfoArray[i].CommandName.Equals(Name))
                {
                    return CodeInfoArray[i].MinimumBitDonation;
                }
            }

            return -1;
        }
    }
}
