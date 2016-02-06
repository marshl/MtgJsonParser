namespace MtgJsonParser
{
    using System.Linq;

    /// <summary>
    /// The main program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point the program
        /// </summary>
        /// <param name="args">The arguments to the program.</param>
        public static void Main(string[] args)
        {
            bool downloadFile = args.Contains("--download") || args.Contains("-d");

            Parser p = new Parser(true, false);
        }
    }
}
