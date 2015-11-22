namespace MtgJsonParser
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a single set on a card that has a list of such Sets.
    /// </summary>
    public class Set
    {
        /// <summary>
        /// Gets or sets the name of the set
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the abbreviated code
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the code that Gatherer uses for the set. Only present if different than 'code'
        /// </summary>
        [JsonProperty("gathererCode")]
        public string GathererCode { get; set; }

        /// <summary>
        /// Gets or sets an old style code used by some Magic software. Only present if different than 'gathererCode' and 'code'
        /// </summary>
        [JsonProperty("oldCode")]
        public string OldCode { get; set; }

        /// <summary>
        /// Gets or sets a code that magiccards.info uses for the set. Only present if magiccards.info has this set
        /// </summary>
        [JsonProperty("magicCardsInfoCode")]
        public string MagicCardsInfoCode { get; set; }

        /// <summary>
        /// Gets or sets the date the set was released (YYYY-MM-DD). For promo sets, the date the first card was released.
        /// </summary>
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the type of border on the cards, either "white", "black" or "silver"
        /// </summary>
        [JsonProperty("border")]
        public string Border { get; set; }

        /// <summary>
        /// Gets or sets the type of the set. 
        ///     One of: "core", "expansion", "reprint", "box", "un",
        ///             "from the vault", "premium deck", "duel deck",
        ///             "starter", "commander", "planechase", "archenemy",
        ///             "promo", "vanguard", "masters"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the block this set is in,
        /// </summary>
        [JsonProperty("block")]
        public string Block { get; set; }

        /// <summary>
        /// Gets or sets list of cards within this set
        /// </summary>
        [JsonProperty("cards")]
        public List<Card> Cards { get; set; }
    }
}
