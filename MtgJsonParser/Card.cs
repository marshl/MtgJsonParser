using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;

namespace MtgJsonParser
{
    public class Card
    {
        /// <summary>
        /// A unique id for this card. It is made up by doing an SHA1 hash of setCode + cardName + cardImageName
        /// </summary>
        [JsonProperty("id")]
        public string ID { get; set; }

        /// <summary>
        /// The card layout. Possible values: normal, split, flip, double-faced, token, plane, scheme, phenomenon, leveler, vanguard
        /// </summary>
        [JsonProperty("layout")]
        public string Layout { get; set; }

        /// <summary>
        /// The card name. For split, double-faced and flip cards, just the name of one side of the card. Basically each 'sub-card' has its own record.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Only used for split, flip and double-faced cards. Will contain all the names on this card, front or back.
        /// </summary>
        [JsonProperty("names")]
        public List<string> Names { get; set; }

        /// <summary>
        /// The mana cost of this card. Consists of one or more mana symbols.
        /// </summary>
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }

        /// <summary>
        /// Converted mana cost. Always a number. NOTE: cmc may have a decimal point as cards from unhinged may contain "half mana" (such as 'Little Girl' with a cmc of 0.5). Cards without this field have an implied cmc of zero as per rule 202.3a
        /// </summary>
        [JsonProperty("cmc")]
        public float CMC { get; set; }

        /// <summary>
        /// The card colors. Usually this is derived from the casting cost, but some cards are special (like the back of double-faced cards and Ghostfire).
        /// </summary>
        [JsonProperty("colors")]
        public List<string> Colors { get; set; }

        [JsonProperty("colorIdentity")]
        public List<string> ColourIdentity { get; set; }

        /// <summary>
        /// The card type. This is the type you would see on the card if printed today. Note: The dash is a UTF8 'long dash' as per the MTG rules
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The supertypes of the card. These appear to the far left of the card type. Example values: Basic, Legendary, Snow, World, Ongoing
        /// </summary>
        [JsonProperty("supertypes")]
        public List<string> Supertypes { get; set; }

        /// <summary>
        /// The types of the card. These appear to the left of the dash in a card type. Example values: Instant, Sorcery, Artifact, Creature, Enchantment, Land, Planeswalker
        /// </summary>
        [JsonProperty("types")]
        public List<string> Types { get; set; }


        /// <summary>
        /// The subtypes of the card. These appear to the right of the dash in a card type. Usually each word is its own subtype. Example values: Trap, Arcane, Equipment, Aura, Human, Rat, Squirrel, etc.
        /// </summary>
        [JsonProperty("subtypes")]
        public List<string> SubTypes { get; set; }


        /// <summary>
        /// The rarity of the card. Examples: Common, Uncommon, Rare, Mythic Rare, Special, Basic Land
        /// </summary>
        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        /// <summary>
        /// The text of the card. May contain mana symbols and other symbols.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// The flavor text of the card.
        /// </summary>
        [JsonProperty("flavor")]
        public string Flavortext { get; set; }

        /// <summary>
        /// The artist of the card. This may not match what is on the card as MTGJSON corrects many card misprints.
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }


        /// <summary>
        /// The card number. This is printed at the bottom-center of the card in small text. This is a string, not an integer, because some cards have letters in their numbers.
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>
        /// The power of the card. This is only present for creatures. This is a string, not an integer, because some cards have powers like: "1+*"
        /// </summary>
        [JsonProperty("power")]
        public string Power { get; set; }

        /// <summary>
        /// The toughness of the card. This is only present for creatures. This is a string, not an integer, because some cards have toughness like: "1+*"
        /// </summary>
        [JsonProperty("toughness")]
        public string Toughness { get; set; }

        /// <summary>
        /// The loyalty of the card. This is only present for planeswalkers.
        /// </summary>
        [JsonProperty("loyalty")]
        public string Loyalty { get; set; }

        /// <summary>
        /// The multiverseid of the card on Wizard's Gatherer web page.
        /// Cards from sets that do not exist on Gatherer will NOT have a multiverseid.
        /// Sets not on Gatherer are: ATH, ITP, DKM, RQS, DPA and all sets with a 4 letter code that starts with a lowercase 'p'.
        /// </summary>
        [JsonProperty("multiverseid")]
        public string MultiverseID { get; set; }

        /// <summary>
        /// If a card has alternate art (for example, 4 different Forests, or the 2 Brothers Yamazaki) then each other variation's multiverseid will be listed here, NOT including the current card's multiverseid. NOTE: Only present for sets that exist on Gatherer.
        /// </summary>
        [JsonProperty("variations")]
        public List<string> Variations { get; set; }

        /// <summary>
        /// This used to refer to the mtgimage.com file name for this card.
        /// mtgimage.com has been SHUT DOWN by Wizards of the Coast.
        /// This field will continue to be set correctly and is now only useful for UID purposes.
        /// </summary>
        [JsonProperty("imageName")]
        public string ImageName { get; set; }

        /// <summary>
        /// If the border for this specific card is DIFFERENT than the border specified in the top level set JSON, then it will be specified here. (Example: Unglued has silver borders, except for the lands which are black bordered)
        /// </summary>
        [JsonProperty("border")]
        public string Border { get; set; }

        /// <summary>
        /// If this card was a timeshifted card in the set.
        /// </summary>
        [JsonProperty("timeshifted")]
        public bool IsTimeshifted { get; set; }

        /// <summary>
        /// Maximum hand size modifier. Only exists for Vanguard cards.
        /// </summary>
        [JsonProperty("hand")]
        public int HandMod { get; set; }

        /// <summary>
        /// Starting life total modifier. Only exists for Vanguard cards.
        /// </summary>
        [JsonProperty("life")]
        public int LifeMod { get; set; }

        /// <summary>
        /// Set to true if this card is reserved by Wizards Official Reprint Policy
        /// </summary>
        [JsonProperty("reserved")]
        public bool IsReserved { get; set; }

        /// <summary>
        /// The date this card was released. This is only set for promo cards. The date may not be accurate to an exact day and month, thus only a partial date may be set (YYYY-MM-DD or YYYY-MM or YYYY). Some promo cards do not have a known release date.
        /// </summary>
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Set to true if this card was only released as part of a core box set. These are technically part of the core sets and are tournament legal despite not being available in boosters.
        /// </summary>
        [JsonProperty("starter")]
        public bool Starter { get; set; }

        /// <summary>
        /// The rulings for the card. An array of objects, each object having 'date' and 'text' keys.
        /// </summary>
        [JsonProperty("rulings")]
        public List<Ruling> Rulings { get; set; }

        /// <summary>
        /// Foreign language names for the card, if this card in this set was printed in another language. An array of objects, each object having 'language', 'name' and 'multiverseid' keys. Not available for all sets.
        /// </summary>
        [JsonProperty("foreignNames")]
        public List<ForeignName> ForeignName { get; set; }

        /// <summary>
        /// The sets that this card was printed in, expressed as an array of set codes.
        /// </summary>
        [JsonProperty("printings")]
        public List<string> Printings { get; set; }

        /// <summary>
        /// The original text on the card at the time it was printed. This field is not available for promo cards.
        /// </summary>
        [JsonProperty("originalText")]
        public string OriginalText { get; set; }

        /// <summary>
        /// The original type on the card at the time it was printed. This field is not available for promo cards.
        /// </summary>
        [JsonProperty("originalType")]
        public string OriginalType { get; set; }

        /// <summary>
        /// Which formats this card is legal, restricted or banned in. An array of objects, each object having 'format' and 'legality'. A 'condition' key may be added in the future if Gatherer decides to utilize it again.
        /// </summary>
        [JsonProperty("legalities")]
        public List<FormatLegality> Legalities { get; set; }

        /// <summary>
        /// For promo cards, this is where this card was originally obtained. For box sets that are theme decks, this is which theme deck the card is from. For clash packs, this is which deck it is from.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        public int OracleID { get; set; }

        public Set ParentSet { get; set; }

        public int? LinkID { get; set; }

        public int ColorFlags
        {
            get
            {
                if (this.Colors == null)
                {
                    return 0;
                }

                int flags = 0;
                foreach (string str in this.Colors)
                {
                    Colour c = Array.Find((Colour[])Enum.GetValues(typeof(Colour)), s => s.GetName().Equals(str, StringComparison.OrdinalIgnoreCase));
                    flags |= c.GetFlagValue();
                }

                return flags;
            }
        }

        public int ColorIdentityFlags
        {
            get
            {
                if (this.ColourIdentity == null)
                {
                    return 0;
                }

                int flags = 0;
                foreach (string str in this.ColourIdentity)
                {
                    Colour c = Array.Find((Colour[])Enum.GetValues(typeof(Colour)), s => s.GetSymbol().Equals(str, StringComparison.OrdinalIgnoreCase));
                    flags |= c.GetFlagValue();
                }

                return flags;
            }
        }

        public int NumPower
        {
            get
            {
                if (this.Power == null)
                {
                    return 0;
                }

                Regex numRegex = new Regex("(-?\\d+)");
                Match match = numRegex.Match(this.Power);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
                return 0;
            }
        }

        public int NumToughness
        {
            get
            {
                if (this.Toughness == null)
                {
                    return 0;
                }

                Regex numRegex = new Regex("(-?\\d+)");
                Match match = numRegex.Match(this.Toughness);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
                return 0;
            }
        }

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

        public List<string> GetOracleChunks()
        {
            List<string> output = new List<string>();
            output.Add(this.OracleID.ToString());
            output.Add(this.Name);
            output.Add(this.ManaCost);
            output.Add(this.CMC.ToString());
            output.Add(this.ColorFlags.ToString());
            output.Add(this.ColorIdentityFlags.ToString());
            output.Add(this.Colors != null ? this.Colors.Count.ToString() : "0");
            output.Add(string.Join(" ", this.Supertypes != null ? string.Join(" ", this.Supertypes) : null, this.Types != null ? string.Join(" ", this.Types) : null));
            output.Add(this.SubTypes != null ? string.Join(" ", this.SubTypes) : null);
            output.Add(this.Power);
            output.Add(this.NumPower.ToString());
            output.Add(this.Toughness);
            output.Add(this.NumToughness.ToString());
            output.Add(this.Loyalty);
            output.Add(this.Text != null ? this.Text.Replace('\n', '~') : null);

            return output;
        }

        public string GetOracleLine()
        {
            StringBuilder strBldr = new StringBuilder();

            List<string> oracleChunks = this.GetOracleChunks();
            for (int i = 0; i < oracleChunks.Count; ++i)
            {
                string chunk = oracleChunks[i];
                if (chunk == null)
                {
                    strBldr.Append("\\N");
                }
                else
                {
                    strBldr.Append(chunk);
                }

                if (i != oracleChunks.Count - 1)
                {
                    strBldr.Append('\t');
                }
            }

            return strBldr.ToString();
        }
    }
}
