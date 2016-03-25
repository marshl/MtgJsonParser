﻿namespace MtgJsonParser
{
    using System;
    using System.Collections.Generic;

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

    /// <summary>
    /// Extends the Colour enum with attribute fetch methods.
    /// </summary>
    public static class ColourExtensions
    {
        /// <summary>
        /// Gets the flag attribute value of the given colour.
        /// </summary>
        /// <param name="colour">The colour enum to get the flag for.</param>
        /// <returns>The flah of the given colour.</returns>
        public static int GetFlagValue(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourFlag>(colour).Value;
        }

        /// <summary>
        /// Gets the symbol attribute value of the given colour
        /// </summary>
        /// <param name="colour">The colour enum to get the symbol for.</param>
        /// <returns>The symbol of the given colour.</returns>
        public static string GetSymbol(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourSymbol>(colour).Value;
        }

        /// <summary>
        /// Gets the name attribute value of the given colour
        /// </summary>
        /// <param name="colour">The colour enum to get the name for.</param>
        /// <returns>The name of the given colour.</returns>
        public static string GetName(this Colour colour)
        {
            return EnumExtensions.GetAttribute<ColourName>(colour).Value;
        }

        public static int ConvertStringsToFlags(List<string> colours)
        {
            int flags = 0;
            var colourEnums = (Colour[])Enum.GetValues(typeof(Colour));
            foreach (string str in colours)
            {
                Colour c = Array.Find(colourEnums, s => s.GetSymbol().Equals(str, StringComparison.OrdinalIgnoreCase));
                flags |= c.GetFlagValue();
            }

            return flags;
        }
    }

    /// <summary>
    /// The binary flag for a colour
    /// </summary>
    public class ColourFlag : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColourFlag"/> class.
        /// </summary>
        /// <param name="value">The flag value to use.</param>
        public ColourFlag(int value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of this flag
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// The single character symbol of a colour.
    /// </summary>
    public class ColourSymbol : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColourSymbol"/> class.
        /// </summary>
        /// <param name="value">The symbol character to use.</param>
        public ColourSymbol(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of this colour symbol.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// The name of a colour
    /// </summary>
    public class ColourName : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColourName"/> class.
        /// </summary>
        /// <param name="value">The name to use.</param>
        public ColourName(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of this name attribute
        /// </summary>
        public string Value { get; set; }
    }
}
