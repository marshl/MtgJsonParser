//-----------------------------------------------------------------------
// <copyright file="FormatLegality.cs" company="marshl">
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
    /// The legality of a card in a certain format.
    /// </summary>
    public class FormatLegality
    {
        /// <summary>
        /// Gets or sets the name of the format.
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets whether the card is legal in this format or not. "Legal" if it is, "Banned" if it is not.
        /// </summary>
        [JsonProperty("legality")]
        public string Legality { get; set; }
    }
}