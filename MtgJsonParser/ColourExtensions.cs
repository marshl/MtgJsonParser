//-----------------------------------------------------------------------
// <copyright file="ColourExtensions.cs" company="marshl">
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

    /// <summary>
    /// Extends the Colour enum with attribute fetch methods.
    /// </summary>
    public static class ColourExtensions
    {
        /// <summary>
        /// Gets the flag attribute value of the given colour.
        /// </summary>
        /// <param name="colour">The colour enum to get the flag for.</param>
        /// <returns>The flah of the given colour.</returns>
        public static int GetFlagValue(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourFlag>(colour).Value;
        }

        /// <summary>
        /// Gets the symbol attribute value of the given colour
        /// </summary>
        /// <param name="colour">The colour enum to get the symbol for.</param>
        /// <returns>The symbol of the given colour.</returns>
        public static string GetSymbol(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourSymbol>(colour).Value;
        }

        /// <summary>
        /// Gets the name attribute value of the given colour
        /// </summary>
        /// <param name="colour">The colour enum to get the name for.</param>
        /// <returns>The name of the given colour.</returns>
        public static string GetName(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourName>(colour).Value;
        }

        /// <summary>
        /// Converts a list of colour strings to bit flags and returns the combined result.
        /// </summary>
        /// <param name="colours">The list of colours to parse/</param>
        /// <returns>The combined flags of all the given colours.</returns>
        public static int ConvertStringsToFlags(List<string> colours)
        {
            int flags = 0;
            var colourEnums = (Colour[])Enum.GetValues(typeof(Colour));
            foreach (string str in colours)
            {
                Colour c = Array.Find(colourEnums, s => s.GetSymbol().Equals(str, StringComparison.OrdinalIgnoreCase));
                flags |= c.GetFlagValue();
            }

            return flags;
        }
    }
}
