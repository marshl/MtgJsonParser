//-----------------------------------------------------------------------
// <copyright file="Rarity.cs" company="marshl">
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
    /// The num for the different rarities in Magic.
    /// </summary>
    public enum Rarity
    {
        [RarityName("basic land")]
        [RaritySymbol('L')]
        Land,

        [RarityName("common")]
        [RaritySymbol('C')]
        Common,

        [RarityName("uncommon")]
        [RaritySymbol('U')]
        Uncommon,

        [RarityName("rare")]
        [RaritySymbol('R')]
        Rare,

        [RarityName("mythic rare")]
        [RaritySymbol('M')]
        MythicRare,

        [RarityName("special")]
        [RaritySymbol('S')]
        Special,
    }

    public class RarityName : Attribute
    {
        public string Value { get; set; }

        public RarityName(string value)
        {
            this.Value = value;
        }
    }

    public class RaritySymbol : Attribute
    {
        public char Value { get; set; }

        public RaritySymbol(char value)
        {
            this.Value = value;
        }
    }

    public static class RarityExtensions
    {
        public static string GetName(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RarityName>(rarity).Value;
        }

        public static char GetSymbol(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RaritySymbol>(rarity).Value;
        }

        public static Rarity GetRarityWithName(string name)
        {
            return Array.Find((Rarity[])Enum.GetValues(typeof(Rarity)), x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
