namespace MtgJsonParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

        const string InputDictionaryFile = @"C:\Users\Liam\Common\dictionary.txt";
        const string OutputDictionaryFile = @"C:\Users\Liam\AppData\Roaming\Microsoft\UProof\MTG.DIC";

        /// <summary>
        /// The list of sets that do not need to be included in the final data set
        /// </summary>
        public List<string> setsToSkip;

        private Dictionary<string, List<string>> additionalBlocks;

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
            this.LoadAdditionalBlocks();
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
            this.WriteDictionaryFile();

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

        private void WriteDictionaryFile()
        {
            HashSet<string> realWordSet = new HashSet<string>();
            using (StreamReader dictionaryReader = new StreamReader(InputDictionaryFile))
            {
                string line;
                while ((line = dictionaryReader.ReadLine()) != null)
                {
                    realWordSet.Add(line.ToLower());
                }
            }
            Console.WriteLine("Done");

            Console.Write("Finding unique names... ");
            HashSet<string> names = new HashSet<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    string[] chunks = card.Name.Split(' ');
                    foreach (string chunk in chunks)
                    {
                        string newChunk = chunk.Replace(",", "").Replace(":", "");
                        if (!realWordSet.Contains(newChunk.ToLower()))
                        {
                            names.Add(newChunk);
                        }
                    }
                }
            }

            using (StreamWriter dictionaryOut = new StreamWriter(OutputDictionaryFile, false, Encoding.Unicode))
            {
                foreach (string name in names)
                {
                    dictionaryOut.WriteLine(name);
                }
            }
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

        private void LoadAdditionalBlocks()
        {
            this.additionalBlocks = new Dictionary<string, List<string>>();
            StreamReader reader = new StreamReader("data/additional_blocks.txt");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] strs = line.Split('\t');
                Debug.Assert(strs.Length > 1);
                string blockname = strs[0].Trim();
                List<string> sets = strs[1].Split(' ').ToList();
                sets.ForEach(x => x = x.Trim());
                additionalBlocks.Add(blockname, sets);
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

        /// <summary>
        /// Finds all the different types and supertypes and writes them to a file.
        /// </summary>
        private void WriteTypesToFile()
        {
            HashSet<string> typeSet = new HashSet<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                // Ignore Big Furry monster, as it has some weird types
                foreach (Card card in set.Cards.FindAll(x => x.Name != "B.F.M. (Big Furry Monster)"))
                {
                    card.Types?.ForEach(x => typeSet.Add(x));
                    card.Supertypes?.ForEach(x => typeSet.Add(x));
                }
            }

            Console.WriteLine("Writing types table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/types", false, utf8WithoutBOM);

            foreach (string type in typeSet)
            {
                sw.WriteLine($"\\N\t{type}");
            }

            sw.Close();
        }

        /// <summary>
        /// Writes all unique subtypes out to a load file.
        /// </summary>
        private void WriteSubtypesToFile()
        {
            HashSet<string> subtypeSet = new HashSet<string>();
            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                foreach (Card card in set.Cards)
                {
                    card.SubTypes?.ForEach(x => subtypeSet.Add(x));
                }
            }

            Console.WriteLine("Writing subtypes table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/subtypes", false, utf8WithoutBOM);

            foreach (string subtype in subtypeSet)
            {
                sw.WriteLine($"\\N\t{subtype}");
            }

            sw.Close();
        }

        /// <summary>
        /// Downloads all set symbol files to 
        /// </summary>
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
                    string url = $"http://gatherer.wizards.com/Handlers/Image.ashx?type=symbol&set={set.Code}&size=small&rarity={c}";
                    string filename = Path.Combine(imageDir.FullName, $"{set.Code}_{c}_small.jpg");
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
                        // Swallow
                    }
                }
            }

        }

        private void CreateBackups()
        {
            Console.WriteLine("Creating backup files.");

            DirectoryInfo backupDir = Directory.CreateDirectory(Path.Combine("backup", string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now)));

            string oldbackup = $"{backupDir.FullName}\\magic_db.sql";
            Process p = Process.Start("CMD.exe", $"/C mysqldump -u root magic_db > \"{oldbackup}\"");

            p.WaitForExit();

            string newbackup = $"{backupDir.FullName}\\delverdb.sql";
            p = Process.Start("CMD.exe", $"/C mysqldump -u root delverdb > \"{newbackup}\"");

            p.WaitForExit();

            System.IO.Compression.ZipFile.CreateFromDirectory(backupDir.FullName, $"backup\\{backupDir.Name}.zip");

            backupDir.Delete(true);
        }

        private void DownloadCardImages()
        {
            Console.WriteLine("Downloading card images.");

            WebClient wc = new WebClient();
            DirectoryInfo imageDir = new DirectoryInfo(@"C:\wamp\www\delverdb\images\cards");

            foreach (KeyValuePair<string, Set> pair in setDictionary)
            {
                foreach (Card card in pair.Value.Cards.FindAll(x => x.MultiverseID != null))
                {
                    string filename = Path.Combine(imageDir.FullName, card.MultiverseID + ".png");

                    if (File.Exists(filename))
                    {
                        continue;
                    }

                    string url = $"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={card.MultiverseID}&type=card";
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

        /// <summary>
        /// 
        /// </summary>
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

                foreach (string linkName in card.Names.FindAll(x => x != card.Name))
                {
                    Card other = this.uniqueCards[linkName];
                    sw.WriteLine($"{card.OracleID}\t{other.OracleID}\t{card.LinkType}");
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

                    str.Append(Nvl(card.Flavortext?.Replace("\n", "~")));
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

                blockMap.Add(set.Block, id);
                ++id;
            }

            foreach (KeyValuePair<string, List<string>> pair in this.additionalBlocks)
            {
                blockMap.Add(pair.Key, id);
                ++id;
            }

            foreach (KeyValuePair<string, int> pair in this.blockMap)
            {
                sw.WriteLine($"{pair.Value}\t{pair.Key}");
            }

            sw.Close();
        }

        /// <summary>
        /// Writes the set list out to a loading file.
        /// </summary>
        private void WriteSetsToFile()
        {
            Console.WriteLine("Writing cardsets table file.");
            Encoding utf8WithoutBOM = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter("temp/sets", false, utf8WithoutBOM);

            foreach (KeyValuePair<string, Set> pair in this.setDictionary)
            {
                Set set = pair.Value;

                if (setsToSkip.Contains(set.Code))
                {
                    continue;
                }

                StringBuilder str = new StringBuilder();

                str.Append("\\N\t");

                str.Append(set.Code);
                str.Append('\t');

                str.Append(set.Name);
                str.Append('\t');

                if (string.IsNullOrWhiteSpace(set.Block))
                {
                    bool inAdditionalBlock = false;
                    foreach (KeyValuePair<string, List<string>> blockPair in this.additionalBlocks)
                    {
                        if (blockPair.Value.Contains(set.Code))
                        {
                            str.Append(blockMap[blockPair.Key]);
                            inAdditionalBlock = true;
                        }
                    }

                    if (!inAdditionalBlock)
                    {
                        str.Append("\\N");
                    }
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

        public string Nvl(string str)
        {
            return str ?? "\\N";
        }
    }

}
