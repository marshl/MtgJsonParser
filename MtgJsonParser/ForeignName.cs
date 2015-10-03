using Newtonsoft.Json;

namespace MtgJsonParser
{
    /// <summary>
    /// Foreign language names for the card, if this card in this set was printed in another language. An array of objects, each object having 'language', 'name' and 'multiverseid' keys. Not available for all sets.
    /// </summary>
    public class ForeignName
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("multiverseid")]
        public string MultiverseID { get; set; }
    }
}
