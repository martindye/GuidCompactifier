using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GUIDCompactifier
{

    public class CompactifyException : Exception
    {
        public CompactifyException(string message) : base(message)
        {

        }
    }

    public class Engine
    {

        const string base64CharString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        public static string GetUncompactedGUID(string originalCompactedGUID)
        {

            // byte[] compactedGUIDBytes = System.Text.Encoding.ASCII.GetBytes(compactedGUID);

            string compactedGUID = originalCompactedGUID;
            string lastChar = compactedGUID.Substring(compactedGUID.Length - 1);
            int lastByteVal = GetBase64CharValue(lastChar);
            BitArray parityBits = IntToBitArray(lastByteVal);
            BitArray adjustedBits = IntToBitArray(lastByteVal);

            // Reset parity bits
            adjustedBits.Set(0, false);
            adjustedBits.Set(1, false);
            adjustedBits.Set(2, false);
            adjustedBits.Set(3, false);

            int newLastByteVal = BitArrayToInt(adjustedBits);
            string newLastChar = GetBase64CharFromValue(newLastByteVal);

            // Remove last char
            compactedGUID = compactedGUID.Remove(compactedGUID.Length - 1, 1);

            // Replace with what would have been the original before it was encoded
            compactedGUID += newLastChar;
            compactedGUID += "==";

            byte[] unCompactedGUIDBytes = Convert.FromBase64String(compactedGUID);
            Guid returnGUID = new Guid(unCompactedGUIDBytes);

            // Perform parity check here
            BitArray inputBits = new BitArray(unCompactedGUIDBytes);
            bool[] parityCheck = new bool[4];
            bool parityPassed = true;

            for (int blockCounter = 0; blockCounter <= 3; blockCounter++)
            {
                bool actualParity = GetParity(inputBits, blockCounter);
                bool recordedParity = parityBits[blockCounter];

                if (actualParity != recordedParity) parityPassed = false;
                parityCheck[blockCounter] = actualParity == recordedParity;
            };

            if (!parityPassed)
            {
                bool[] badDigits = new bool[22];

                // Throw exception here
                for (int blockCounter = 0; blockCounter <= 3; blockCounter++)
                {

                    // TODO: Move this into the above block?
                    if (!parityCheck[blockCounter])
                    {
                        int byteStartIndex = (blockCounter * 4) + 1;
                        int byteEndIndex = byteStartIndex + 3;

                        int base64StartIndex = GetBase64CharIndexStart(byteStartIndex);
                        int base64EndIndex = GetBase64CharIndexStart(byteEndIndex) + 1;

                        for (int digitCounter = base64StartIndex - 1; digitCounter < base64EndIndex; digitCounter++)
                        {
                            badDigits[digitCounter] = true;
                        };

                    };

                };

                // The last Base64 digit is used for parity checks, so if the parity fails, this
                // is always a digit that could be incorrect.
                badDigits[21] = true;

                string exceptionMsg = "Parity Error in GUID:" + Environment.NewLine;

                exceptionMsg += originalCompactedGUID + Environment.NewLine;
                for (int badDigitCounter = 0; badDigitCounter <= 21; badDigitCounter++)
                {
                    exceptionMsg += badDigits[badDigitCounter] ? "^" : " ";
                };

                throw new CompactifyException(exceptionMsg);

            };

            return returnGUID.ToString();
        }

        /// <summary>
        /// When some bytes are encoded to base 64, pass in the original byte position
        /// to return the base 64 char index here.
        /// Note that 4 base 64 characters represent 3 bytes, so each byte is covered
        /// by 2 base 64 chars. Add 1 to the returned index to get the second base 64
        /// char index covered by the byte number passed in
        /// </summary>
        /// <remarks>
        /// Formula to find base 64 char no. from byte no.
        /// ((Byte - 1) / 3 (int) ) * 4 + ( [Remainder of (Byte - 1) / 3] + 1 )
        /// </remarks>
        /// <param name="byteNumber">
        /// 1-based index of byte number
        /// </param>
        /// <returns>
        /// 1-based index of base 64 char number
        /// </returns>
        public static int GetBase64CharIndexStart(int byteNumber)
        {
            int result = ((byteNumber - 1) / 3) * 4 + (byteNumber - 1) % 3 + 1;
            return result;
        }

        public static string GetCompactedGUID(Guid inputGUID)
        {
            string base64GUID;
            string compactedGUID;

            byte[] inputBytes = inputGUID.ToByteArray();
            base64GUID = Convert.ToBase64String(inputBytes);

            // Remove last 2 chars "=="
            base64GUID = base64GUID.Substring(0, 22);
            byte[] outputBytes = Encoding.ASCII.GetBytes(base64GUID);

            BitArray inputBits = new BitArray(inputBytes);

            byte lastByte = outputBytes.Last();
            string lastChar = Encoding.ASCII.GetString(new byte[] { lastByte });
            int lastByteToInt = GetBase64CharValue(lastChar);


            // BitArray lastCharBits = new BitArray(new byte[] { lastByte });
            BitArray lastCharBits = new BitArray(new byte[] { (byte)lastByteToInt });

            for (int blockCounter = 0; blockCounter <= 3; blockCounter++)
            {

                bool oddParity = GetParity(inputBits, blockCounter);

                // We only need to alter the last 6 bits (1 x Base64 char).
                // Change bits 4 to 6 according to the parity of bytes 1-4, 5-8, 9-12 and 13-16
                // lastCharBits[2 + blockCounter] = oddParity;
                lastCharBits[blockCounter] = oddParity;
            };

            byte[] newLastByte = new byte[1];
            lastCharBits.CopyTo(newLastByte, 0);
            int lastByteVal = (int)newLastByte[0];
            string newLastBase64Char = GetBase64CharFromValue(lastByteVal);

            // Replace the last character with the tweaked one containing the extra info
            outputBytes[outputBytes.Length - 1] = Encoding.ASCII.GetBytes(newLastBase64Char)[0];

            compactedGUID = System.Text.Encoding.ASCII.GetString(outputBytes);

            return compactedGUID;
        }

        public static int BitArrayToInt(BitArray input)
        {
            byte[] newByte = new byte[1];
            input.CopyTo(newByte, 0);
            return (int)newByte[0];
        }

        public static BitArray IntToBitArray(int input)
        {
            BitArray newBitArray = new BitArray(new byte[] { (byte)input });
            return newBitArray;
        }

        public static int GetBase64CharValue(string base64Char)
        {
            int charPos = base64CharString.IndexOf(base64Char);
            return charPos;
        }

        public static string GetBase64CharFromValue(int value)
        {
            string base64Char = base64CharString.Substring(value, 1);
            return base64Char;
        }

        public static bool GetParity(BitArray inputBits, int blockNumber)
        {
            // Size of block of bits, for which to check the parity
            const int blockSize = 32;

            int onBitCount = (from bool bitToCheck in inputBits
                              select bitToCheck).Skip(blockNumber * blockSize).Take(blockSize)
                  .Count(bitToCheck => bitToCheck == true);

            bool parity = (onBitCount - (onBitCount / 2) * 2) != 0;

            return parity;
        }

    }

}
