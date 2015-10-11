using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgJsonParser
{
    class Program
    {
        static void Main(string[] args)
        {
            bool downloadFile = args.Contains("--download") || args.Contains("-d");

            Parser p = new Parser(downloadFile, false);
        }
    }
}
