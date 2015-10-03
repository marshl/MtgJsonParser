using Newtonsoft.Json;

namespace MtgJsonParser
{
    public class FormatLegality
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("legality")]
        public string Legality { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("multiverseid")]
        public string MultiverseID { get; set; }
    }
}