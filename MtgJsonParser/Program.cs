//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="marshl">
// Copyright 2016, Liam Marshall, marshl.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------
namespace MtgJsonParser
{
    using System;
    using System.Collections.Generic;
    using Mono.Options;

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
            bool showHelp = false;
            bool forceDownload = false;
            bool refreshDelverFromMagicDb = false;
            bool refreshTutelageFromDelver = false;
            bool pushToDelverDb = false;
            bool pushToTutelage = false;

            OptionSet optionSet = new OptionSet()
            {
                { "d|download",
                    "Whether to download a new copy of the Json data or not\n",
                  v => forceDownload = v != null },
                 { "rd|refresh_delver",
                    "Whether to refresh delverdb from magic_db or not\n",
                  v => refreshDelverFromMagicDb = v != null },
                { "rt|refresh_tutelage",
                    "Whether to refresh tutelage from delverdb\n",
                  v => refreshTutelageFromDelver = v != null },
                { "pd|push_to_delver",
                    "Whether to update the delver database with changes\n",
                  v => pushToDelverDb = v != null },
                { "pt|push_to_tutelage",
                    "Whether to update the tutelage database with changes\n",
                  v => pushToTutelage = v != null },
                { "h|help",  "show this message and exit",
                  v => showHelp = v != null },
            };

            List<string> extraOptions;
            try
            {
                extraOptions = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("MtgJsonParser: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `MtgJsonParser --help' for more information.");
                return;
            }

            if (showHelp)
            {
                Console.WriteLine("Options:");
                optionSet.WriteOptionDescriptions(Console.Out);
                return;
            }

            string message;
            if (extraOptions.Count > 0)
            {
                message = string.Join(" ", extraOptions.ToArray());
                Console.WriteLine($"Unknown argument(s): {message}");
                return;
            }

            Parser p = new Parser(
                downloadFile: forceDownload,
                refreshDelverFromOldData: refreshDelverFromMagicDb,
                refreshTutelageFromDelver: refreshTutelageFromDelver,
                pushToDelverDb: pushToDelverDb,
                pushToTutelage: pushToTutelage);
        }
    }
}
