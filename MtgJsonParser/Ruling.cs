namespace MtgJsonParser
{
    using Newtonsoft.Json;

    /// <summary>
    /// The rulings for the card. An array of objects, each object having 'date' and 'text' keys.
    /// </summary>
    public class Ruling
    {
        /// <summary>
        /// Gets or sets the date this ruling was made
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets the text of this ruling
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
