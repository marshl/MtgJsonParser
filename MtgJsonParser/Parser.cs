using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MtgJsonParser.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MtgJsonParser
{
    public class Parser
    {
        public Parser(bool downloadFile)
        {
            if (downloadFile)
            {
                WebClient wc = new WebClient();
                wc.DownloadFile(Settings.Default.JsonUrl, Settings.Default.JsonZipFilename);
                System.IO.Compression.ZipFile.ExtractToDirectory(Settings.Default.JsonZipFilename, Settings.Default.JsonDirectory);
            }



            Dictionary<string, Set> setDictionary;
            {
                string data = File.ReadAllText(Path.Combine(Settings.Default.JsonDirectory, Settings.Default.JsonFilename));
                setDictionary = JsonConvert.DeserializeObject<Dictionary<string, Set>>(data);
            }
            return;
        }
    }

    

    /*
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CardLayout
    {
        [EnumMember(Value = "normal")]
        Normal,

        [EnumMember(Value = "split")]
        Split,

        [EnumMember(Value = "flip")]
        Flip,

        [EnumMember(Value = "double-faced")]
        DoubleFaced,

        [EnumMember(Value = "token")]
        Token,

        [EnumMember(Value = "plane")]
        Plane,

        [EnumMember(Value = "scheme")]
        Scheme,

        [EnumMember(Value = "phenomenon")]
        Phenomenon,

        [EnumMember(Value = "leveler")]
        Leveler,

        [EnumMember(Value = "vangarud")]
        Vanguard,
    }*/
}
