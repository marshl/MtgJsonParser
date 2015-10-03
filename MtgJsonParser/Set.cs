using System.Collections.Generic;
using Newtonsoft.Json;

namespace MtgJsonParser
{
    public class Set
    {
        /// <summary>
        /// The name of the set
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The set's abbreviated code
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// The code that Gatherer uses for the set. Only present if different than 'code'
        /// </summary>
        [JsonProperty("gathererCode")]
        public string GathererCode { get; set; }

        /// <summary>
        /// An old style code used by some Magic software. Only present if different than 'gathererCode' and 'code'
        /// </summary>
        [JsonProperty("oldCode")]
        public string OldCode { get; set; }

        /// <summary>
        /// The code that magiccards.info uses for the set. Only present if magiccards.info has this set
        /// </summary>
        [JsonProperty("magicCardsInfoCode")]
        public string MagicCardsInfoCode { get; set; }

        /// <summary>
        /// When the set was released (YYYY-MM-DD). For promo sets, the date the first card was released.
        /// </summary>
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// The type of border on the cards, either "white", "black" or "silver"
        /// </summary>
        [JsonProperty("border")]
        public string Border { get; set; }

        /// <summary>
        /// Type of set. One of: "core", "expansion", "reprint", "box", "un",
        ///                      "from the vault", "premium deck", "duel deck",
        ///                      "starter", "commander", "planechase", "archenemy",
        ///                      "promo", "vanguard", "masters"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The block this set is in,
        /// </summary>
        [JsonProperty("block")]
        public string Block { get; set; }

        /// <summary>
        /// The block this set is in,
        /// </summary>
        [JsonProperty("cards")]
        public List<Card> Cards { get; set; }
    }
}
