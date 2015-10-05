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
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace MtgJsonParser
{
    public class Parser
    {
        private Dictionary<string, Set> setDictionary;
        Dictionary<string, Card> uniqueCards = new Dictionary<string, Card>();

        public Parser(bool downloadFile, bool refreshFromOldData)
        {
            if (downloadFile)
            {
                WebClient wc = new WebClient();
                wc.DownloadFile(Settings.Default.JsonUrl, Settings.Default.JsonZipFilename);
                System.IO.Compression.ZipFile.ExtractToDirectory(Settings.Default.JsonZipFilename, Settings.Default.JsonDirectory);
            }

            this.ReadSetData();
            this.SetOracleIDs();
            Directory.CreateDirectory("temp");
            Directory.CreateDirectory("backup");


            MySqlConnection con = new MySqlConnection();
            con.ConnectionString = "server=" + Settings.Default.MySqlServer
                                + ";user=" + Settings.Default.MySqlUser
                                + (string.IsNullOrEmpty(Settings.Default.MySqlPassword) ? null : ":password=" + Settings.Default.MySqlPassword)
                                + ";database=" + Settings.Default.MySqlDatabase
                                + ";port=" + Settings.Default.MySqlPort;

            con.Open();


            /*IEnumerable<Set> orderedSets =
                from s in setDictionary.Values
                orderby s.ReleaseDate ascending
                select s;*/

            this.WriteCardsTable();
            this.WriteCardSetsTable();

            this.DownloadCardImages();

            string backupfile = "backup/" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + "magic_db.sql";
            Process.Start("CMD.exe", "/C mysqldump -u root magic_db > " + backupfile);

            backupfile = "backup/" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + "delverdb.sql";
            Process.Start("CMD.exe", "/C mysqldump -u root delverdb > " + backupfile);

            MySqlCommand command = con.CreateCommand();

            command.CommandText = "TRUNCATE oldtonewcards";
            command.ExecuteNonQuery();

            command.CommandText = "TRUNCATE historicalcards";
            command.ExecuteNonQuery();

            command.CommandText = "DROP TABLE IF EXISTS historicalcards";
            command.ExecuteNonQuery();

            if (refreshFromOldData)
            {
                command.CommandText = @"
                CREATE TABLE historicalcards
                AS (SELECT cardid id, name FROM magic_db.oracle)";
                command.ExecuteNonQuery();

                command.CommandText = "TRUNCATE usercards";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO usercards ( SELECT * FROM magic_db.usercards)";
                command.ExecuteNonQuery();


                command.CommandText = "TRUNCATE usercardchanges";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO usercardchanges ( SELECT * FROM magic_db.usercardlog)";
                command.ExecuteNonQuery();

                command.CommandText = "TRUNCATE taglinks";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO taglinks ( SELECT * FROM magic_db.taglinks)";
                command.ExecuteNonQuery();

                command.CommandText = "TRUNCATE deckcards";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO deckcards ( SELECT * FROM magic_db.deckcards)";
                command.ExecuteNonQuery();

            }
            else
            {
                command.CommandText = @"
                   CREATE TABLE historicalcards
                   AS (SELECT * FROM cards)";
                command.ExecuteNonQuery();
            }

            command.CommandText = "TRUNCATE cards";
            command.ExecuteNonQuery();

            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/cards' INTO TABLE cards LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();

            command.CommandText = @"
            INSERT INTO oldtonewcards(
                SELECT hc.id, c.id
                FROM cards c
                JOIN historicalcards hc
                ON hc.name = c.name
            )";
            command.ExecuteNonQuery();

            command.CommandText = "TRUNCATE cardsets";
            command.ExecuteNonQuery();

            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/cardsets' INTO TABLE cardsets LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();

            command.CommandTimeout = 120;
            command.CommandText = "UPDATE usercards SET cardid = ( SELECT newcardid FROM oldtonewcards WHERE oldcardid = usercards.cardid )";
            command.ExecuteNonQuery();

            command.CommandText = "UPDATE usercardchanges SET cardid = ( SELECT newcardid FROM oldtonewcards WHERE oldcardid = usercardchanges.cardid )";
            command.ExecuteNonQuery();

            command.CommandText = "UPDATE taglinks SET cardid = ( SELECT newcardid FROM oldtonewcards WHERE oldcardid = taglinks.cardid )";
            command.ExecuteNonQuery();

            command.CommandText = "UPDATE deckcards SET cardid = ( SELECT newcardid FROM oldtonewcards WHERE oldcardid = deckcards.cardid )";
            command.ExecuteNonQuery();

            command.Dispose();

            con.Close();

            return;
        }

        private void DownloadCardImages()
        {
            WebClient wc = new WebClient();
            Directory.CreateDirectory("img");
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                foreach (Card card in pair.Value.Cards)
                {
                    if ( card.MultiverseID == null )
                    {
                        continue;
                    }

                    string filename = Path.Combine("img", card.MultiverseID + ".png");
                    string url = "http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + card.MultiverseID + "&type=card";
                    if (File.Exists(filename))
                    {
                        continue;
                    }

                    try
                    {
                        wc.DownloadFile(url, filename);
                        Console.WriteLine(url);
                    }
                    catch ( WebException e )
                    {
                        Console.WriteLine("Failed to download " + url + " " + e);
                    }
                }
            }
        }

        private void SetOracleIDs()
        {
            int oracleID = 0;
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                foreach (Card card in pair.Value.Cards)
                {
                    if (!uniqueCards.ContainsKey(card.Name))
                    {
                        card.ParentSet = pair.Value;
                        card.OracleID = ++oracleID;
                        uniqueCards.Add(card.Name, card);
                    }
                }
            }
        }

        private void ReadSetData()
        {
            string data = File.ReadAllText(Path.Combine(Settings.Default.JsonDirectory, Settings.Default.JsonFilename));
            this.setDictionary = JsonConvert.DeserializeObject<Dictionary<string, Set>>(data);
        }

        public void WriteCardsTable()
        {
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/cards", false, utf8WithoutBOM);
            foreach (KeyValuePair<string, Card> pair in this.uniqueCards)
            {
                Card card = pair.Value;
                sw.WriteLine(card.GetOracleLine());
            }
            sw.Close();
        }

        public void WriteCardSetsTable()
        {
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/cardsets", false, utf8WithoutBOM);

            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;
                foreach (Card card in set.Cards)
                {
                    StringBuilder str = new StringBuilder();
                    str.Append("\\N\t");
                    str.Append(card.OracleID);
                    str.Append('\t');

                    str.Append(set.Code);
                    str.Append('\t');

                    str.Append(card.MultiverseID);
                    str.Append('\t');

                    str.Append(card.Artist);
                    str.Append('\t');

                    str.Append(!string.IsNullOrWhiteSpace(card.Flavortext) ? card.Flavortext.Replace("\n", "~") : "\\N");
                    str.Append('\t');

                    str.Append(MtgConstants.RarityToChar(MtgConstants.ParseRarity(card.Rarity)));
                    str.Append('\t');

                    str.Append(card.Number);

                    sw.WriteLine(str.ToString());
                }
            }

            sw.Close();
            Console.WriteLine("Done");
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
