using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgJsonParser
{
    /// <summary>
    /// 
    /// </summary>
    public enum Rarity
    {
        [RarityName("basic land")]
        [RaritySymbol('L')]
        Land,

        [RarityName("common")]
        [RaritySymbol('C')]
        Common,

        [RarityName("uncommon")]
        [RaritySymbol('U')]
        Uncommon,

        [RarityName("rare")]
        [RaritySymbol('R')]
        Rare,

        [RarityName("mythic rare")]
        [RaritySymbol('M')]
        MythicRare,

        [RarityName("special")]
        [RaritySymbol('S')]
        Special,
    }

    public class RarityName : Attribute
    {
        public string Value { get; set; }

        public RarityName(string value)
        {
            this.Value = value;
        }
    }

    public class RaritySymbol : Attribute
    {
        public char Value { get; set; }

        public RaritySymbol(char value)
        {
            this.Value = value;
        }
    }

    public static class RarityExtensions
    {
        public static string GetName(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RarityName>(rarity).Value;
        }

        public static char GetSymbol(this Rarity rarity)
        {
            return EnumExtensions.GetAttribute<RaritySymbol>(rarity).Value;
        }

        public static Rarity GetRarityWithName(string name)
        {
            return Array.Find((Rarity[])Enum.GetValues(typeof(Rarity)), x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
