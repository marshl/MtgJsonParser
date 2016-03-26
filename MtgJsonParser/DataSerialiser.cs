//-----------------------------------------------------------------------
// <copyright file="DataSerialiser.cs" company="marshl">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A static class for serialiser card inforamtion into files to then load via SQL Load
    /// </summary>
    public static class DataSerialiser
    {
        /// <summary>
        /// Serialises the card specific information.
        /// </summary>
        /// <param name="card">The card to serialise.</param>
        /// <returns>The serialised card information.</returns>
        public static string SerialiseCard(Card card)
        {
            var parts = GetPartsForCard(card);
            return JoinObjectParts(parts);
        }

        /// <summary>
        /// Serialises the set specific information about a card.
        /// </summary>
        /// <param name="card">The card to serialise.</param>
        /// <param name="set">The set the card is from.</param>
        /// <returns>The serialised card set information.</returns>
        public static string SerialiseCardSet(Card card, Set set)
        {
            var parts = GetPartsForCardSet(card, set);
            return JoinObjectParts(parts);
        }

        /// <summary>
        /// Gets all the items in the card that will be loaded into the database.
        /// </summary>
        /// <param name="card">The card to parse.</param>
        /// <returns>A string for each column in the card table.</returns>
        private static List<string> GetPartsForCard(Card card)
        {
            string allSuperTypes = card.Supertypes != null ? string.Join(" ", card.Supertypes) : null;
            string allTypes = card.Types != null ? string.Join(" ", card.Types) : null;
            string allSubTypes = card.SubTypes != null ? string.Join(" ", card.SubTypes) : null;

            List<string> output = new List<string>();
            output.Add(card.OracleID.ToString());
            output.Add(card.Name);
            output.Add(card.ManaCost);
            output.Add(card.CMC.ToString());
            output.Add(card.ColorFlags.ToString());
            output.Add(card.ColorIdentityFlags.ToString());
            output.Add(card.Colors != null ? card.Colors.Count.ToString() : "0");
            output.Add(string.Join(" ", allSuperTypes, allTypes));
            output.Add(allSubTypes);
            output.Add(card.Power);
            output.Add(card.NumPower.ToString());
            output.Add(card.Toughness);
            output.Add(card.NumToughness.ToString());
            output.Add(card.Loyalty);
            output.Add(card.Text != null ? card.Text.Replace('\n', '~') : null);

            return output;
        }

        /// <summary>
        /// Gets all the items in the card that are specific to the printing it is in.
        /// </summary>
        /// <param name="card">The card to parse.</param>
        /// <param name="set">The set the card is within.</param>
        /// <returns>The parts of the card.</returns>
        private static List<string> GetPartsForCardSet(Card card, Set set)
        {
            List<string> parts = new List<string>();

            // Set the id column to NULL so it is auto-incremented
            parts.Add(null);
            parts.Add(card.OracleID.ToString());
            parts.Add(set.Code);
            parts.Add(card.MultiverseID);
            parts.Add(card.Artist);
            parts.Add(card.Flavortext?.Replace("\n", "~") ?? "\\N");
            Rarity rarity = RarityExtensions.GetRarityWithName(card.Rarity);
            parts.Add(rarity.GetSymbol().ToString());
            parts.Add(card.Number);

            return parts;
        }

        /// <summary>
        /// Joins the parts of an object to serialise, replacing any null values with SQL NULL
        /// </summary>
        /// <param name="parts">The parts to join.</param>
        /// <returns>The joined string.</returns>
        private static string JoinObjectParts(List<string> parts)
        {
            // Replace any null values with the SQL NULL character
            var processedParts = parts.Select(x => x ?? "\\N").ToList();
            return string.Join("\t", processedParts);
        }
    }
}
