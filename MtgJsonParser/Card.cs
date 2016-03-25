//-----------------------------------------------------------------------
// <copyright file="Card.cs" company="marshl">
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
    using System.Text;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// A card belonging to a single set.
    /// </summary>
    public class Card
    {
        /// <summary>
        /// Gets or sets the unique id for this card. It is made up by doing an SHA1 hash of setCode + cardName + cardImageName
        /// </summary>
        [JsonProperty("id")]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the  card layout. Possible values: normal, split, flip, double-faced, token, plane, scheme, phenomenon, leveler, vanguard
        /// </summary>
        [JsonProperty("layout")]
        public string Layout { get; set; }

        /// <summary>
        /// Gets or sets the card name. For split, double-faced and flip cards, just the name of one side of the card. Basically each 'sub-card' has its own record.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the names of the card. Used only for split, flip and double-faced cards. Will contain all the names on this card, front or back.
        /// </summary>
        [JsonProperty("names")]
        public List<string> Names { get; set; }

        /// <summary>
        /// Gets or sets the mana cost of this card. Consists of one or more mana symbols.
        /// </summary>
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }

        /// <summary>
        /// Gets or sets the converted mana cost. Always a number. NOTE: cmc may have a decimal point as cards from unhinged may contain "half mana" (such as 'Little Girl' with a cmc of 0.5). Cards without this field have an implied cmc of zero as per rule 202.3a
        /// </summary>
        [JsonProperty("cmc")]
        public float CMC { get; set; }

        /// <summary>
        /// Gets or sets the card colors. Usually this is derived from the casting cost, but some cards are special (like the back of double-faced cards and Ghostfire).
        /// </summary>
        [JsonProperty("colors")]
        public List<string> Colors { get; set; }

        /// <summary>
        /// Gets or sets the colour identity. This is created reading all card color information and costs. It is the same for double-sided cards (if they have different colors, the identity will have both colors). It also identifies all mana symbols in the card (cost and text). Mostly used on commander decks.
        /// </summary>
        [JsonProperty("colorIdentity")]
        public List<string> ColourIdentity { get; set; }

        /// <summary>
        /// Gets or sets the entire type line. This is the type you would see on the card if printed today. Note: The dash is a UTF8 'long dash' as per the MTG rules
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the supertypes of the card. These appear to the far left of the card type. Example values: Basic, Legendary, Snow, World, Ongoing
        /// </summary>
        [JsonProperty("supertypes")]
        public List<string> Supertypes { get; set; }

        /// <summary>
        /// Gets or sets the types of the card. These appear to the left of the dash in a card type. Example values: Instant, Sorcery, Artifact, Creature, Enchantment, Land, Planeswalker
        /// </summary>
        [JsonProperty("types")]
        public List<string> Types { get; set; }

        /// <summary>
        /// Gets or sets the subtypes of the card. These appear to the right of the dash in a card type. Usually each word is its own subtype. Example values: Trap, Arcane, Equipment, Aura, Human, Rat, Squirrel, etc.
        /// </summary>
        [JsonProperty("subtypes")]
        public List<string> SubTypes { get; set; }

        /// <summary>
        /// Gets or sets the rarity of the card. Examples: Common, Uncommon, Rare, Mythic Rare, Special, Basic Land
        /// </summary>
        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        /// <summary>
        /// Gets or sets the text of the card. May contain mana symbols and other symbols.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the flavor text of the card.
        /// </summary>
        [JsonProperty("flavor")]
        public string Flavortext { get; set; }

        /// <summary>
        /// Gets or sets the artist of the card. This may not match what is on the card as MTGJSON corrects many card misprints.
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }

        /// <summary>
        /// Gets or sets the card number. This is printed at the bottom-center of the card in small text. This is a string, not an integer, because some cards have letters in their numbers.
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the power of the card. This is only present for creatures. This is a string, not an integer, because some cards have powers like: "1+*"
        /// </summary>
        [JsonProperty("power")]
        public string Power { get; set; }

        /// <summary>
        /// Gets or sets the toughness of the card. This is only present for creatures. This is a string, not an integer, because some cards have toughness like: "1+*"
        /// </summary>
        [JsonProperty("toughness")]
        public string Toughness { get; set; }

        /// <summary>
        /// Gets or sets the loyalty of the card. This is only present for planeswalkers.
        /// </summary>
        [JsonProperty("loyalty")]
        public string Loyalty { get; set; }

        /// <summary>
        /// Gets or sets the multiverseid of the card on Wizard's Gatherer web page.
        /// Cards from sets that do not exist on Gatherer will NOT have a multiverseid.
        /// Sets not on Gatherer are: ATH, ITP, DKM, RQS, DPA and all sets with a 4 letter code that starts with a lowercase 'p'.
        /// </summary>
        [JsonProperty("multiverseid")]
        public string MultiverseID { get; set; }

        /// <summary>
        /// Gets or sets the art variations of the card. If a card has alternate art (for example, 4 different Forests, or the 2 Brothers Yamazaki) then each other variation's multiverseid will be listed here, NOT including the current card's multiverseid. NOTE: Only present for sets that exist on Gatherer.
        /// </summary>
        [JsonProperty("variations")]
        public List<string> Variations { get; set; }

        /// <summary>
        /// Gets or sets the number used to refer to the mtgimage.com file name for this card.
        /// mtgimage.com has been SHUT DOWN by Wizards of the Coast.
        /// This field will continue to be set correctly and is now only useful for UID purposes.
        /// </summary>
        [JsonProperty("imageName")]
        public string ImageName { get; set; }

        /// <summary>
        /// Gets or sets the border variation of the card. If the border for this specific card is DIFFERENT than the border specified in the top level set JSON, then it will be specified here. (Example: Unglued has silver borders, except for the lands which are black bordered)
        /// </summary>
        [JsonProperty("border")]
        public string Border { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this card was a timeshifted card in the set.
        /// </summary>
        [JsonProperty("timeshifted")]
        public bool IsTimeshifted { get; set; }

        /// <summary>
        /// Gets or sets the maximum hand size modifier. Only exists for Vanguard cards.
        /// </summary>
        [JsonProperty("hand")]
        public int HandMod { get; set; }

        /// <summary>
        /// Gets or sets the starting life total modifier. Only exists for Vanguard cards.
        /// </summary>
        [JsonProperty("life")]
        public int LifeMod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this card is reserved by Wizards Official Reprint Policy
        /// </summary>
        [JsonProperty("reserved")]
        public bool IsReserved { get; set; }

        /// <summary>
        /// Gets or sets the date this card was released. This is only set for promo cards. The date may not be accurate to an exact day and month, thus only a partial date may be set (YYYY-MM-DD or YYYY-MM or YYYY). Some promo cards do not have a known release date.
        /// </summary>
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this card was only released as part of a core box set. These are technically part of the core sets and are tournament legal despite not being available in boosters.
        /// </summary>
        [JsonProperty("starter")]
        public bool Starter { get; set; }

        /// <summary>
        /// Gets or sets the rulings for the card. An array of objects, each object having 'date' and 'text' keys.
        /// </summary>
        [JsonProperty("rulings")]
        public List<Ruling> Rulings { get; set; }

        /// <summary>
        /// Gets or sets the foreign language names for the card, if this card in this set was printed in another language. An array of objects, each object having 'language', 'name' and 'multiverseid' keys. Not available for all sets.
        /// </summary>
        [JsonProperty("foreignNames")]
        public List<ForeignName> ForeignName { get; set; }

        /// <summary>
        /// Gets or sets the sets that this card was printed in, expressed as an array of set codes.
        /// </summary>
        [JsonProperty("printings")]
        public List<string> Printings { get; set; }

        /// <summary>
        /// Gets or sets the original text on the card at the time it was printed. This field is not available for promo cards.
        /// </summary>
        [JsonProperty("originalText")]
        public string OriginalText { get; set; }

        /// <summary>
        /// Gets or sets the original type on the card at the time it was printed. This field is not available for promo cards.
        /// </summary>
        [JsonProperty("originalType")]
        public string OriginalType { get; set; }

        /// <summary>
        /// Gets or sets which formats this card is legal, restricted or banned in. An array of objects, each object having 'format' and 'legality'. A 'condition' key may be added in the future if Gatherer decides to utilize it again.
        /// </summary>
        [JsonProperty("legalities")]
        public List<FormatLegality> Legalities { get; set; }

        /// <summary>
        /// Gets or sets where this card was originally obtained (for promo cards). For box sets that are theme decks, this is which theme deck the card is from. For clash packs, this is which deck it is from.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the unique ID for this card (regardless of set)
        /// </summary>
        public int OracleID { get; set; }

        /// <summary>
        /// Gets the binary representation of the colours of the card.
        /// </summary>
        public int ColorFlags
        {
            get
            {
                if (this.Colors == null || this.Colors.Count == 0)
                {
                    return 0;
                }

                return ColourExtensions.ConvertStringsToFlags(this.Colors);
            }
        }

        /// <summary>
        /// Gets the binary representation of the colour identity of this card
        /// </summary>
        public int ColorIdentityFlags
        {
            get
            {
                if (this.ColourIdentity == null || this.ColourIdentity.Count == 0)
                {
                    return 0;
                }

                return ColourExtensions.ConvertStringsToFlags(this.ColourIdentity);
            }
        }

        public float NumPower
        {
            get
            {
                if (this.Power == null)
                {
                    return 0;
                }

                Regex numRegex = new Regex("(-?\\d+)");
                Match match = numRegex.Match(this.Power);
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            }
        }

        /// <summary>
        /// Gets the numeric value of 
        /// </summary>
        public float NumToughness
        {
            get
            {
                if (this.Toughness == null)
                {
                    return 0;
                }

                Regex numRegex = new Regex("(-?\\d+)");
                Match match = numRegex.Match(this.Toughness);
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            }
        }

        /// <summary>
        /// Gets the type of card link for the card, if applicable.
        /// </summary>
        public string LinkType
        {
            get
            {
                if (this.Names == null)
                {
                    return null;
                }

                switch (this.Layout)
                {
                    case "split":
                        return "s";
                    case "flip":
                        return "f";
                    case "double-faced":
                        return "t";
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Gets all the items in the card that will be loaded into the database.
        /// </summary>
        /// <returns>A string for each column in the card table.</returns>
        public List<string> GetOracleChunks()
        {
            string allSuperTypes = this.Supertypes != null ? string.Join(" ", this.Supertypes) : null;
            string allTypes = this.Types != null ? string.Join(" ", this.Types) : null;
            string allSubTypes = this.SubTypes != null ? string.Join(" ", this.SubTypes) : null;

            List<string> output = new List<string>();
            output.Add(this.OracleID.ToString());
            output.Add(this.Name);
            output.Add(this.ManaCost);
            output.Add(this.CMC.ToString());
            output.Add(this.ColorFlags.ToString());
            output.Add(this.ColorIdentityFlags.ToString());
            output.Add(this.Colors != null ? this.Colors.Count.ToString() : "0");
            output.Add(string.Join(" ", allSuperTypes, allTypes));
            output.Add(allSubTypes);
            output.Add(this.Power);
            output.Add(this.NumPower.ToString());
            output.Add(this.Toughness);
            output.Add(this.NumToughness.ToString());
            output.Add(this.Loyalty);
            output.Add(this.Text != null ? this.Text.Replace('\n', '~') : null);

            return output;
        }

        /// <summary>
        /// Creates a string to the loaded by the database containing all information for the card.
        /// </summary>
        /// <returns>The tab separated data columns of this card. </returns>
        public string GetOracleLine()
        {
            List<string> oracleChunks = this.GetOracleChunks();

            // Replace any null values with the SQL NULL character
            oracleChunks = oracleChunks.Select(x => x ?? "\\N").ToList();
            return string.Join("\t", oracleChunks);
        }
    }
}
