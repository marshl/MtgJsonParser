using System.Linq;

namespace MtgJsonParser
{
    class Program
    {
        static void Main(string[] args)
        {
            bool downloadFile = args.Contains("--download") || args.Contains("-d");

            Parser p = new Parser(false, false);
        }
    }
}
