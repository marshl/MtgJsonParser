﻿//-----------------------------------------------------------------------
// <copyright file="Ruling.cs" company="marshl">
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
