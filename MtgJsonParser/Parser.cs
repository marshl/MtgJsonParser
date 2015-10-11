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
        Dictionary<string, int> blockMap;

        List<string> typeList;
        List<string> subtypeList;

        public string[] setsToSkip = {
            "CEI",      // International Collector"s Edition (1993-12-01)
            "CED",      // Collector"s Edition (1993-12-01)
            "pDRC",     // Dragon Con (1994-01-01)
            "pMEI",     // Media Inserts (1995-01-01)
            "pLGM",     // Legend Membership (1995-01-01)
            "RQS",      // Rivals Quick Start Set (1996-07-01)
            "pARL",     // Arena League (1996-08-02)
            "pCEL",     // Celebration (1996-08-14)
            "MGB",      // Multiverse Gift Box (1996-11-01)
            "ITP",      // Introductory Two-Player Set (1996-12-31)
            "pPOD",     // Portal Demo Game (1997-05-01)
            "VAN",      // Vanguard (1997-05-01)
            "pPRE",     // Prerelease Events (1997-10-04)
            "pJGP",     // Judge Gift Program (1998-06-01)
            "pALP",     // Asia Pacific Land Program (1998-09-01)
            "ATH",      // Anthologies (1998-11-01)
            "pGRU",     // Guru (1999-07-12)
            "pWOR",     // Worlds (1999-08-04)
            "pWOS",     // Wizards of the Coast Online Store (1999-09-04)
            "pSUS",     // Super Series (1999-12-01)
            "pFNM",     // Friday Night Magic (2000-02-01)
            "pELP",     // European Land Program (2000-02-05)
            "pMPR",     // Magic Player Rewards (2001-05-01)
            "DKM",      // Deckmasters (2001-12-01)
            "pREL",     // Release Events (2003-07-26)
            "p2HG",     // Two-Headed Giant Tournament (2005-12-09)
            "pGTW",     // Gateway (2006-01-01)
            "pCMP",     // Champs and States (2006-03-18)
            "CST",      // Coldsnap Theme Decks (2006-07-21)
            "pHHO",     // Happy Holidays (2006-12-31)
            "pPRO",     // Pro Tour (2007-02-09)
            "pGPX",     // Grand Prix (2007-02-24)
            "pMGD",     // Magic Game Day (2007-07-14)
            "pSUM",     // Summer of Magic (2007-07-21)
            "pLPA",     // Launch Parties (2008-02-01)
            "p15A",     // 15th Anniversary (2008-04-01)
            "pWPN",     // Wizards Play Network (2008-10-01)
            "pWCQ",     // World Magic Cup Qualifiers (2013-04-06)
            "CPK",      // Clash Pack (2014-07-18)
            "FRF_UGIN", // Ugin"s Fate promos (2015-01-17)
        };

        public Parser(bool downloadFile, bool refreshFromOldData)
        {
            if (downloadFile)
            {
                Console.WriteLine("Downloading data file.");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(Settings.Default.JsonUrl, Settings.Default.JsonZipFilename);
                }
                Console.WriteLine("Unzipping data file.");
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

            this.WriteCardsTable();
            this.WriteCardSetsTable();
            this.WriteBlocksToFile();
            this.WriteSetsToFile();
            this.WriteSubtypesToFile();
            this.WriteTypesToFile();
            this.WriteCardLinksTable();

            this.DownloadCardImages();
            this.CreateBackups();
            this.DownloadSetSymbols();

            MySqlCommand command = con.CreateCommand();

            command.CommandText = "TRUNCATE oldtonewcards";
            command.ExecuteNonQuery();

            command.CommandText = "TRUNCATE historicalcards";
            command.ExecuteNonQuery();


            if (refreshFromOldData)
            {
                Console.WriteLine("Refreshing data set from magic_db database.");

                command.CommandText = @"
                INSERT INTO historicalcards
                (SELECT cardid id, name FROM magic_db.oracle)";
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
                   INSERT INTO historicalcards
                   (SELECT id, name FROM cards)";
                command.ExecuteNonQuery();
            }

            command.CommandText = "TRUNCATE cards";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading cards table into database.");
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

            Console.WriteLine("Loading cardsets table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/cardsets' INTO TABLE cardsets LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();


            command.CommandText = "TRUNCATE blocks";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading blocks table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/blocks' INTO TABLE blocks LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();


            command.CommandText = "TRUNCATE sets";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading sets table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/sets' INTO TABLE sets LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();


            command.CommandText = "TRUNCATE types";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading types table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/types' INTO TABLE types LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();


            command.CommandText = "TRUNCATE subtypes";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading subtypes table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/subtypes' INTO TABLE subtypes LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();

            command.CommandText = "TRUNCATE cardlinks";
            command.ExecuteNonQuery();

            Console.WriteLine("Loading card links table into database.");
            command.CommandText = "LOAD DATA LOCAL INFILE 'temp/links' INTO TABLE cardlinks LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();


            Console.WriteLine("Updating old card IDs with new values.");
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

        private void WriteTypesToFile()
        {
            this.typeList = new List<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    if (card.Types == null)
                    {
                        continue;
                    }

                    foreach (string type in card.Types)
                    {
                        if (!typeList.Contains(type))
                        {
                            this.typeList.Add(type);
                        }
                    }
                }
            }

            Console.WriteLine("Writing types table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/types", false, utf8WithoutBOM);

            foreach (string type in this.typeList)
            {
                sw.WriteLine("\\N\t" + type);
            }

            sw.Close();
        }

        private void WriteSubtypesToFile()
        {
            this.subtypeList = new List<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    if (card.SubTypes == null)
                    {
                        continue;
                    }

                    foreach (string subtype in card.SubTypes)
                    {
                        if (!subtypeList.Contains(subtype))
                        {
                            this.subtypeList.Add(subtype);
                        }
                    }
                }
            }

            Console.WriteLine("Writing subtypes table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/subtypes", false, utf8WithoutBOM);

            foreach (string subtype in this.subtypeList)
            {
                sw.WriteLine("\\N\t" + subtype);
            }

            sw.Close();
        }

        private void DownloadSetSymbols()
        {
            Console.WriteLine("Downloading setcodes");

            WebClient wc = new WebClient();
            DirectoryInfo imageDir = new DirectoryInfo(@"C:\wamp\www\delverdb\images\exp");

            //char[] rarities = { 'C', 'U', 'R', 'M', 'L', 'S' };

            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                List<char> rarities = new List<char>();

                foreach (Card card in set.Cards)
                {
                    char rarity = MtgConstants.RarityToChar(MtgConstants.ParseRarity(card.Rarity));
                    if (!rarities.Contains(rarity))
                    {
                        rarities.Add(rarity);
                    }
                }

                foreach (char c in rarities)
                {
                    string url = "http://gatherer.wizards.com/Handlers/Image.ashx?type=symbol&set=" + set.Code + "&size=small&rarity=" + c;
                    string filename = Path.Combine(imageDir.FullName, set.Code + "_" + c + "_small.jpg");
                    if (File.Exists(filename))
                    {
                        continue;
                    }

                    try
                    {
                        Console.WriteLine(filename);
                        wc.DownloadFile(url, filename);
                    }
                    catch (WebException)
                    {
                        //swallow
                    }
                }
            }

        }

        private void CreateBackups()
        {
            Console.WriteLine("Creating backup files.");
            string backupfile = "backup/" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + "magic_db.sql";
            Process.Start("CMD.exe", "/C mysqldump -u root magic_db > " + backupfile);

            backupfile = "backup/" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + "delverdb.sql";
            Process.Start("CMD.exe", "/C mysqldump -u root delverdb > " + backupfile);
        }

        private void DownloadCardImages()
        {
            Console.WriteLine("Downloading card images.");

            WebClient wc = new WebClient();
            DirectoryInfo imageDir = new DirectoryInfo(@"C:\wamp\www\delverdb\images\cards");

            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                foreach (Card card in pair.Value.Cards)
                {
                    if (card.MultiverseID == null)
                    {
                        continue;
                    }

                    string filename = Path.Combine(imageDir.FullName, card.MultiverseID + ".png");
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
                    catch (WebException e)
                    {
                        Console.WriteLine("Failed to download " + url + " " + e);
                    }
                }
            }
        }

        private void SetOracleIDs()
        {
            Console.WriteLine("Setting oracle IDs");

            int oracleID = 0;
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;
                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    card.ParentSet = pair.Value;
                    if (!uniqueCards.ContainsKey(card.Name))
                    {
                        card.OracleID = ++oracleID;
                        uniqueCards.Add(card.Name, card);
                    }
                    else
                    {
                        card.OracleID = uniqueCards[card.Name].OracleID;
                    }
                }
            }
        }

        private void ReadSetData()
        {
            Console.WriteLine("Reading set data.");
            string data = File.ReadAllText(Path.Combine(Settings.Default.JsonDirectory, Settings.Default.JsonFilename));

            Console.WriteLine("Deserialising set data");
            this.setDictionary = JsonConvert.DeserializeObject<Dictionary<string, Set>>(data);
        }

        private void WriteCardLinksTable()
        {
            Console.WriteLine("Writing cards table file.");

            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/links", false, utf8WithoutBOM);
            foreach (KeyValuePair<string, Card> pair in this.uniqueCards)
            {
                Card card = pair.Value;

                if (card.Names == null)
                {
                    continue;
                }

                foreach (string linkName in card.Names)
                {
                    if ( linkName == card.Name )
                    {
                        continue;
                    }

                    Card other = this.uniqueCards[linkName];
                    sw.WriteLine(card.OracleID + "\t" + other.OracleID + "\t" + card.LinkType);
                }
            }
            sw.Close();
        }

        public void WriteCardsTable()
        {
            Console.WriteLine("Writing cards table file.");

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
            Console.WriteLine("Writing cardsets table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/cardsets", false, utf8WithoutBOM);

            foreach (KeyValuePair<string, Set> pair in this.setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

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

        private void WriteBlocksToFile()
        {
            Console.WriteLine("Writing cardsets table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/blocks", false, utf8WithoutBOM);

            int id = 1;
            this.blockMap = new Dictionary<string, int>();

            foreach (KeyValuePair<string, Set> pair in this.setDictionary)
            {
                Set set = pair.Value;

                if (set.Block == null || blockMap.ContainsKey(set.Block))
                {
                    continue;
                }

                sw.WriteLine(id + "\t" + set.Block);
                blockMap.Add(set.Block, id);
                ++id;
            }

            sw.Close();
        }

        private void WriteSetsToFile()
        {
            Console.WriteLine("Writing cardsets table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/sets", false, utf8WithoutBOM);

            foreach (KeyValuePair<string, Set> pair in this.setDictionary)
            {
                Set set = pair.Value;
                StringBuilder str = new StringBuilder();

                str.Append("\\N\t");

                str.Append(set.Code);
                str.Append('\t');

                str.Append(set.Name);
                str.Append('\t');

                if (string.IsNullOrWhiteSpace(set.Block))
                {
                    str.Append("\\N");
                }
                else
                {
                    str.Append(blockMap[set.Block]);
                }

                str.Append('\t');

                str.Append(set.ReleaseDate);

                sw.WriteLine(str.ToString());
            }
            sw.Close();
        }
    }

}
