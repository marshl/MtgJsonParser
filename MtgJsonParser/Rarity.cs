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
        /// <summary>
        /// The land rarity
        /// </summary>
        [RarityName("basic land")]
        [RaritySymbol('L')]
        Land,

        /// <summary>
        /// The common rarity
        /// </summary>
        [RarityName("common")]
        [RaritySymbol('C')]
        Common,

        /// <summary>
        /// The uncommon rarity
        /// </summary>
        [RarityName("uncommon")]
        [RaritySymbol('U')]
        Uncommon,

        /// <summary>
        /// The rare rarity
        /// </summary>
        [RarityName("rare")]
        [RaritySymbol('R')]
        Rare,

        /// <summary>
        /// The mythic rare rarity
        /// </summary>
        [RarityName("mythic rare")]
        [RaritySymbol('M')]
        MythicRare,

        /// <summary>
        /// The special rarity (such as timeshifted)
        /// </summary>
        [RarityName("special")]
        [RaritySymbol('S')]
        Special,
    }
}
