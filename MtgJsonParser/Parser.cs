namespace MtgJsonParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using Properties;
    using Newtonsoft.Json;
    using MySql.Data.MySqlClient;
    using System.Diagnostics;

    /// <summary>
    /// 
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, Set> setDictionary;


        Dictionary<string, Card> uniqueCards = new Dictionary<string, Card>();
        Dictionary<string, int> blockMap;

        /// <summary>
        /// The list of sets that do not need to be included in the final data set
        /// </summary>
        public List<string> setsToSkip;

        public Parser(bool downloadFile, bool refreshFromOldData)
        {
            this.LoadSetsToSkip();

            if (downloadFile)
            {
                Console.WriteLine("Downloading data file.");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(Settings.Default.JsonUrl, Settings.Default.JsonZipFilename);
                }
                Console.WriteLine("Unzipping data file.");
                Directory.Delete(Settings.Default.JsonDirectory, true);
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

            this.LoadLocalFileIntoTable("cards", "temp/cards", command);

            command.CommandText = "TRUNCATE oldtonewcards";
            command.ExecuteNonQuery();

            command.CommandText = @"
            INSERT INTO oldtonewcards(
                SELECT hc.id, c.id
                FROM cards c
                JOIN historicalcards hc
                ON hc.name = c.name
            )";
            command.ExecuteNonQuery();

            this.LoadLocalFileIntoTable("cardsets", "temp/cardsets", command);
            this.LoadLocalFileIntoTable("blocks", "temp/blocks", command);
            this.LoadLocalFileIntoTable("sets", "temp/sets", command);
            this.LoadLocalFileIntoTable("types", "temp/types", command);
            this.LoadLocalFileIntoTable("subtypes", "temp/subtypes", command);
            this.LoadLocalFileIntoTable("cardlinks", "temp/links", command);

            Console.WriteLine("Updating old card IDs with new values.");

            this.UpdateTableWithNewCardIDs("usercards", command);
            this.UpdateTableWithNewCardIDs("usercardchanges", command);
            this.UpdateTableWithNewCardIDs("taglinks", command);
            this.UpdateTableWithNewCardIDs("deckcards", command);

            command.Dispose();

            con.Close();
        }

        /// <summary>
        /// Loads the file that contains the list of sets to skip and stores it in a list.
        /// </summary>
        private void LoadSetsToSkip()
        {
            this.setsToSkip = new List<string>();
            StreamReader reader = new StreamReader("data/sets_to_skip.txt");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string setcode = line.Split('#')[0].Trim();
                this.setsToSkip.Add(setcode);
            }
        }

        /// <summary>
        /// Updates the usercard IDs in the given table name with new IDs, using the oldtonewcards table.
        /// </summary>
        /// <param name="table">The table to update.</param>
        /// <param name="command">The command to update the table with.</param>
        private void UpdateTableWithNewCardIDs(string table, MySqlCommand command)
        {
            command.CommandTimeout = 120;
            command.CommandText = $"UPDATE {table} t JOIN oldtonewcards o ON o.oldcardid = t.cardid SET t.cardid = o.newcardid";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads the contents of a local file into the given table.
        /// </summary>
        /// <param name="tablename">The name of the table to load the data into.</param>
        /// <param name="filename">The name of the local file to load the data from.</param>
        /// <param name="command">The MySQL command to perform the update with.</param>
        private void LoadLocalFileIntoTable(string tablename, string filename, MySqlCommand command)
        {
            Console.WriteLine($"Truncating {tablename}");
            command.CommandText = $"TRUNCATE {tablename}";
            command.ExecuteNonQuery();

            Console.WriteLine($"Loading {tablename} table into database.");
            command.CommandText = $"LOAD DATA LOCAL INFILE '{filename}' INTO TABLE {tablename} LINES TERMINATED BY '\r\n'";
            command.ExecuteNonQuery();
        }

        private void WriteTypesToFile()
        {
            List<string> typeList = new List<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    if (card.Types != null)
                    {
                        foreach (string type in card.Types)
                        {
                            if (!typeList.Contains(type))
                            {
                                typeList.Add(type);
                            }
                        }
                    }

                    if (card.Supertypes != null)
                    {
                        foreach (string supertype in card.Supertypes)
                        {
                            if (!typeList.Contains(supertype))
                            {
                                typeList.Add(supertype);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Writing types table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/types", false, utf8WithoutBOM);

            foreach (string type in typeList)
            {
                sw.WriteLine("\\N\t" + type);
            }

            sw.Close();
        }

        private void WriteSubtypesToFile()
        {
            List<string> subtypeList = new List<string>();
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
                            subtypeList.Add(subtype);
                        }
                    }
                }
            }

            Console.WriteLine("Writing subtypes table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/subtypes", false, utf8WithoutBOM);

            foreach (string subtype in subtypeList)
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
                    Rarity rarity = RarityExtensions.GetRarityWithName(card.Rarity);
                    if (!rarities.Contains(rarity.GetSymbol()))
                    {
                        rarities.Add(rarity.GetSymbol());
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
                        // Swaller
                    }
                }
            }

        }

        private void CreateBackups()
        {
            Console.WriteLine("Creating backup files.");

            DirectoryInfo backupDir = Directory.CreateDirectory(Path.Combine("backup", string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now)));

            string oldbackup = backupDir.FullName + "\\magic_db.sql";
            Process p = Process.Start("CMD.exe", "/C mysqldump -u root magic_db > \"" + oldbackup + "\"");

            p.WaitForExit();

            string newbackup = backupDir.FullName + "\\delverdb.sql";
            p = Process.Start("CMD.exe", "/C mysqldump -u root delverdb > \"" + newbackup + "\"");

            p.WaitForExit();
            //System.Threading.Thread.Sleep(500);

            System.IO.Compression.ZipFile.CreateFromDirectory(backupDir.FullName, "backup\\" + backupDir.Name + ".zip");
            //string cmd = "7z a backup\\" + backupDir.Name + ".7z \"" + backupDir.FullName + "\"";
            //p = Process.Start("CMD.exe", "/C " + cmd);

            //p.WaitForExit();

            //System.Threading.Thread.Sleep(500);
            backupDir.Delete(true);
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
                    if (linkName == card.Name)
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

                    Rarity rarity = RarityExtensions.GetRarityWithName(card.Rarity);
                    str.Append(rarity.GetSymbol());
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
