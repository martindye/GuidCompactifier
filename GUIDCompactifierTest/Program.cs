using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GUIDCompactifier
{
    class Program
    {
        static void Main(string[] args)
        {

            GUIDTester myTest = new GUIDTester();
            string result = myTest.RunTest();

            Console.WriteLine("Original GUID = " + myTest.TestGUID);
            Console.WriteLine("Compacted GUID = " + result);
            Console.WriteLine("De-Compacted GUID = " + myTest.DecompactedGUID);
            Console.WriteLine("Try your own: ");
            myTest.CompactedGUID = Console.ReadLine();

            Console.WriteLine("Compacted GUID = " + myTest.CompactedGUID);

            try
            {
                myTest.TryDecompact();
                Console.WriteLine("De-Compacted GUID = " + myTest.DecompactedGUID);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            };

            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }
    }

    public class GUIDTester
    {
        public Guid TestGUID { get; set; }
        public string DecompactedGUID { get; set; }
        public string CompactedGUID { get; set; }

        public string RunTest()
        {
            string result;

            this.TestGUID = Guid.NewGuid();

            result = Engine.GetCompactedGUID(this.TestGUID);

            this.DecompactedGUID = Engine.GetUncompactedGUID(result);

            return result;
        }

        public string TryDecompact()
        {
            this.DecompactedGUID = Engine.GetUncompactedGUID(this.CompactedGUID);
            return this.DecompactedGUID;
        }
    }
}
