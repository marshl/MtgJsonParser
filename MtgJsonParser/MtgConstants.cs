using System;

namespace MtgJsonParser
{
    class MtgConstants
    {
        public enum RARITY
        {
            LAND,
            COMMON,
            UNCOMMON,
            RARE,
            MYTHIC_RARE,
            SPECIAL,
        }

        public static RARITY ParseRarity(string input)
        {
            switch (input.ToUpper())
            {
                case "LAND":
                case "BASIC LAND":
                    return RARITY.LAND;
                case "COMMON":
                    return RARITY.COMMON;
                case "UNCOMMON":
                    return RARITY.UNCOMMON;
                case "RARE":
                    return RARITY.RARE;
                case "MYTHIC RARE":
                    return RARITY.MYTHIC_RARE;
                case "SPECIAL":
                    return RARITY.SPECIAL;
                default:
                    throw new ArgumentException("Unknown rarity: " + input);
            }
        }

        public static char RarityToChar(RARITY rarity)
        {
            switch (rarity)
            {
                case RARITY.LAND:
                    return 'L';
                case RARITY.COMMON:
                    return 'C';
                case RARITY.UNCOMMON:
                    return 'U';
                case RARITY.RARE:
                    return 'R';
                case RARITY.MYTHIC_RARE:
                    return 'M';
                case RARITY.SPECIAL:
                    return 'S';
                default:
                    throw new ArgumentException("Unknown rarity " + rarity);
            }
        }

        public static char ColourNameToCode(string color)
        {
            switch (color.ToUpper())
            {
                case "WHITE":
                    return 'W';
                case "BLUE":
                    return 'U';
                case "BLACK":
                    return 'B';
                case "RED":
                    return 'R';
                case "GREEN":
                    return 'G';
                default:
                    throw new ArgumentException("Unknown colour " + color);
            }
        }

        public static char[] COLOUR_CODES = { 'W', 'U', 'B', 'R', 'G', };

        public static int ColourCodeToFlag(char colorCode)
        {
            switch (colorCode)
            {
                case 'W':
                    return 1 << 0;
                case 'U':
                    return 1 << 1;
                case 'B':
                    return 1 << 2;
                case 'R':
                    return 1 << 3;
                case 'G':
                    return 1 << 4;
                default:
                    throw new ArgumentException("Unknown colour code " + colorCode);
            }

        }
    }
}
