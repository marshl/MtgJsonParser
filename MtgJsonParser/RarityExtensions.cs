//-----------------------------------------------------------------------
// <copyright file="RarityExtensions.cs" company="marshl">
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

    /// <summary>
    /// Extension for the rarity enum.
    /// </summary>
    public static class RarityExtensions
    {
        /// <summary>
        /// Gets the name of the rarity.
        /// </summary>
        /// <param name="rarity">The rarity to get the name of.</param>
        /// <returns>The name of the rarity.</returns>
        public static string GetName(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RarityName>(rarity).Value;
        }

        /// <summary>
        /// Gets the symbol of a rarity.
        /// </summary>
        /// <param name="rarity">The rarity to get the symbol of..</param>
        /// <returns>The symbol of the rarity.</returns>
        public static char GetSymbol(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RaritySymbol>(rarity).Value;
        }

        /// <summary>
        /// Gets the rarity that matches the given name.
        /// </summary>
        /// <param name="name">The name of the rarity to search for.</param>
        /// <returns>The rarity with the given name.</returns>
        public static Rarity GetRarityWithName(string name)
        {
            return Array.Find((Rarity[])Enum.GetValues(typeof(Rarity)), x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
