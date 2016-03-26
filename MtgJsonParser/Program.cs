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

            Parser p = new Parser(false, false);
        }
    }
}
