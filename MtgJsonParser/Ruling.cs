using Newtonsoft.Json;

namespace MtgJsonParser
{
    /// <summary>
    /// The rulings for the card. An array of objects, each object having 'date' and 'text' keys.
    /// </summary>
    public class Ruling
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
