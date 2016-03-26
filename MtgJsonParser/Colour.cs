//-----------------------------------------------------------------------
// <copyright file="Colour.cs" company="marshl">
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
    using System;

    /// <summary>
    /// All the colours of magic
    /// </summary>
    public enum Colour
    {
        /// <summary>
        /// The colour white
        /// </summary>
        [ColourFlag(1 << 0)]
        [ColourSymbol("W")]
        [ColourName("WHITE")]
        White,

        /// <summary>
        /// The colour blue
        /// </summary>
        [ColourFlag(1 << 1)]
        [ColourSymbol("U")]
        [ColourName("BLUE")]
        Blue,

        /// <summary>
        /// The colour black
        /// </summary>
        [ColourFlag(1 << 2)]
        [ColourSymbol("B")]
        [ColourName("BLACK")]
        Black,

        /// <summary>
        /// The colour red
        /// </summary>
        [ColourFlag(1 << 3)]
        [ColourSymbol("R")]
        [ColourName("RED")]
        Red,

        /// <summary>
        /// The colour green
        /// </summary>
        [ColourFlag(1 << 4)]
        [ColourSymbol("G")]
        [ColourName("GREEN")]
        Green,
    }
}
