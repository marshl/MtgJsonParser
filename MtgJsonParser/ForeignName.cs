//-----------------------------------------------------------------------
// <copyright file="ForeignName.cs" company="marshl">
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
    using Newtonsoft.Json;

    /// <summary>
    /// Foreign language names for the card, if this card in this set was printed in another language. An array of objects, each object having 'language', 'name' and 'multiverseid' keys. Not available for all sets.
    /// </summary>
    public class ForeignName
    {
        /// <summary>
        /// Gets or sets the language of the card
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the name in the foreign language
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Gatherer multiverse-id of the card of the given language
        /// </summary>
        [JsonProperty("multiverseid")]
        public string MultiverseID { get; set; }
    }
}
