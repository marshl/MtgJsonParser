//-----------------------------------------------------------------------
// <copyright file="Parser.cs" company="marshl">
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
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Common;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;
    using Npgsql;
    using Properties;

    /// <summary>
    /// The main parsing system
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// A mapping of unique setcodes to the set information.
        /// </summary>
        private Dictionary<string, Set> setDictionary;

        /// <summary>
        /// A mapping of unique card names to the card information.
        /// </summary>
        private Dictionary<string, Card> uniqueCards;

        /// <summary>
        /// A mapping of unqiue block names to their ID (unique ascending)
        /// </summary>
        private Dictionary<string, int> blockMap;

        /// <summary>
        /// The list of sets that do not need to be included in the final data set
        /// </summary>
        private List<string> setsToSkip;

        /// <summary>
        /// User defined blocks in addition to those provided in the JSON data
        /// </summary>
        private Dictionary<string, List<string>> additionalBlocks;

        /// <summary>
        /// The file encoding to use for outputting the tab separated files.
        /// </summary>
        private Encoding defaultFileEncoding = new UTF8Encoding(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        /// <param name="downloadFile">Whether to download a new copy of the JSON data or not</param>
        /// <param name="refreshDelverFromOldData">Whether to refresh the usercards data from the old database or not.</param>
        /// <param name="refreshTutelageFromDelver">Whether to refresh the tutelage database from delver.</param>
        /// <param name="pushToDelverDb">Whether to push the data to the DelverDb database.</param>
        /// <param name="pushToTutelage">Whether to push the data to the Tutelage database.</param>
        public Parser(bool downloadFile, bool refreshDelverFromOldData, bool refreshTutelageFromDelver, bool pushToDelverDb, bool pushToTutelage)
        {
            this.setsToSkip = this.LoadSetsToSkip();

            // Download the file data if prompted to, or if the file doesn't already exist
            if (downloadFile || !File.Exists(Path.Combine(Settings.Default.JsonDirectory, Settings.Default.JsonFilename)))
            {
                Console.WriteLine("Downloading data file.");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(Settings.Default.JsonUrl, Settings.Default.JsonZipFilename);
                }

                Directory.Delete(Settings.Default.JsonDirectory, true);

                Console.WriteLine("Unzipping data file.");
                System.IO.Compression.ZipFile.ExtractToDirectory(Settings.Default.JsonZipFilename, Settings.Default.JsonDirectory);
            }

            this.setDictionary = this.ReadJsonData();
            this.additionalBlocks = this.LoadAdditionalBlocks();
            this.SetOracleIDs();
            Directory.CreateDirectory("temp");
            Directory.CreateDirectory("backup");

            this.CreateLoaderFiles(includeIdColumn: pushToDelverDb);

            MySqlConnection mysqlConnection = null;
            MySqlCommand mysqlCommand = null;

            NpgsqlConnection postgresConnection = null;
            NpgsqlCommand postgresCommand = null;

            if (pushToDelverDb || refreshTutelageFromDelver)
            {
                mysqlConnection = new MySqlConnection();
                ConnectionStringSettings conSettings = ConfigurationManager.ConnectionStrings["DelverDbConnection"];
                mysqlConnection.ConnectionString = conSettings.ConnectionString;

                mysqlConnection.Open();
                mysqlCommand = mysqlConnection.CreateCommand();
            }

            if (pushToTutelage)
            {
                postgresConnection = new NpgsqlConnection();
                ConnectionStringSettings conSettings = ConfigurationManager.ConnectionStrings["TutelageConnection"];
                postgresConnection.ConnectionString = conSettings.ConnectionString;
                postgresConnection.Open();
                postgresCommand = postgresConnection.CreateCommand();
            }

            if (pushToTutelage || pushToDelverDb)
            {
                this.CreateDatabaseBackups(
                    backupMagicDb: false,
                    backupDelverDb: pushToDelverDb,
                    backupTutelageDb: pushToTutelage);

                if (pushToDelverDb)
                {
                    if (refreshDelverFromOldData)
                    {
                        this.RefreshDelverFromMagicDb(mysqlCommand);
                    }

                    this.UpdateDatabaseWithCommand(mysqlCommand, refreshDelverFromOldData);
                }

                if (pushToTutelage)
                {
                    if (refreshTutelageFromDelver)
                    {
                        this.RefreshTutelageFromDelver(mysqlCommand, postgresCommand);
                    }

                    this.UpdateDatabaseWithCommand(postgresCommand, refreshTutelageFromDelver);
                }
            }

            if (pushToDelverDb)
            {
                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }

            if (pushToTutelage)
            {
                postgresCommand.Dispose();
                postgresConnection.Close();
            }

            this.DownloadCardImages();
            this.DownloadSetSymbols();
            this.WriteDictionaryFile();
        }

        /// <summary>
        /// Updates a database using the given command.
        /// </summary>
        /// <param name="command">The command to update the database (includes which database to update)</param>
        /// <param name="refreshFromOldData">Whether to update the database with old data.</param>
        private void UpdateDatabaseWithCommand(DbCommand command, bool refreshFromOldData)
        {
            if (!refreshFromOldData)
            {
                command.CommandText = "TRUNCATE historicalcards";
                command.ExecuteNonQuery();

                command.CommandText = @"
                       INSERT INTO historicalcards
                       (SELECT id, name FROM cards)";
                command.ExecuteNonQuery();
            }

            Console.Write("Loading files into database... ");

            this.LoadLocalFileIntoTable("cards", "temp/cards", command, "id,name,cost,cmc,colour,colouridentity,numcolours,type,subtype,power,numpower,toughness,numtoughness,loyalty,rules");
            this.LoadLocalFileIntoTable("cardsets", "temp/cardsets", command, "cardid,setcode,multiverseid,artist,flavourtext,rarity,collectornum");
            this.LoadLocalFileIntoTable("blocks", "temp/blocks", command, "id,name");
            this.LoadLocalFileIntoTable("sets", "temp/sets", command, "id,code,name,blockid,release_date");
            this.LoadLocalFileIntoTable("types", "temp/types", command, "id,name");
            this.LoadLocalFileIntoTable("subtypes", "temp/subtypes", command, "id,name");
            this.LoadLocalFileIntoTable("cardlinks", "temp/links", command, "cardid_from,cardid_to,link_type");

            Console.WriteLine("Done");

            Console.Write("Creating old-to-new-card-id table... ");

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

            Console.WriteLine("Done");

            Console.Write("Updating old card IDs with new values... ");

            this.UpdateTableWithNewCardIDs("usercards", command);
            this.UpdateTableWithNewCardIDs("usercardchanges", command);
            this.UpdateTableWithNewCardIDs("taglinks", command);
            this.UpdateTableWithNewCardIDs("deckcards", command);

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Creates the tab separated table files to be loaded into the database.
        /// </summary>
        /// <param name="includeIdColumn">Whether to include the Id column in the output.</param>
        private void CreateLoaderFiles(bool includeIdColumn)
        {
            this.WriteCardsTable();
            this.WriteCardSetsFile(includeIdColumn);
            this.WriteBlocksToFile();
            this.WriteSetsToFile();
            this.WriteSubtypesToFile();
            this.WriteTypesToFile();
            this.WriteCardLinksTable();
        }

        /// <summary>
        /// Truncates existing user tables, and loads data from the old database.
        /// </summary>
        /// <param name="command">The sql command to use.</param>
        private void RefreshDelverFromMagicDb(DbCommand command)
        {
            Console.Write("Refreshing data set from magic_db database.");

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

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Refrehses the data in the tutelage database from DelverDB
        /// </summary>
        /// <param name="mysqlCommand">The DelverDB command to operate with.</param>
        /// <param name="postgresCommand">The tutelage command to operate with.</param>
        private void RefreshTutelageFromDelver(MySqlCommand mysqlCommand, NpgsqlCommand postgresCommand)
        {
            Directory.CreateDirectory("migrate");
            
            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, ownerid, cardid, setcode, count FROM usercards ORDER BY id ASC", "usercards");
            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, userid, cardid, setcode, DATE_FORMAT(datemodified, '%Y-%c-%e %T' ), difference FROM usercardchanges ORDER BY id ASC", "usercardchanges");

            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, name FROM tags ORDER BY id ASC", "tags");
            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, tagid, cardid FROM taglinks ORDER BY id ASC", "taglinks");

            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, ownerid, deckname, DATE_FORMAT(datecreated, '%Y-%c-%e %T' ), DATE_FORMAT(datemodified, '%Y-%c-%e %T' ) FROM decks ORDER BY id ASC", "decks");
            this.DumpRecordsFromTable(mysqlCommand, "SELECT id, deckid, cardid, count FROM deckcards ORDER BY id ASC", "deckcards");

            this.LoadLocalFileIntoTable("usercards", "migrate/usercards", postgresCommand, "id, ownerid, cardid, setcode, count");
            this.LoadLocalFileIntoTable("usercardchanges", "migrate/usercardchanges", postgresCommand, "id, userid, cardid, setcode, datemodified, difference");
            this.LoadLocalFileIntoTable("tags", "migrate/tags", postgresCommand, "id, name");
            this.LoadLocalFileIntoTable("taglinks", "migrate/taglinks", postgresCommand, "id, tagid, cardid");
            this.LoadLocalFileIntoTable("decks", "migrate/decks", postgresCommand, "id, ownerid, deckname, datecreated, datemodified");
            this.LoadLocalFileIntoTable("deckcards", "migrate/deckcards", postgresCommand, "id, deckid, cardid, count");
        }

        /// <summary>
        /// Dumps the result of a given SQL query into a tab separated file.
        /// </summary>
        /// <param name="command">The database to pull the data from.</param>
        /// <param name="query">The query to select the data with.</param>
        /// <param name="filename">The file to dump the results to.</param>
        private void DumpRecordsFromTable(DbCommand command, string query, string filename)
        {
            command.CommandText = query;
            using (StreamWriter writer = new StreamWriter($"migrate\\{filename}"))
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; ++i)
                    {
                        if (i != 0)
                        {
                            writer.Write('\t');
                        }

                        object val = reader.GetValue(i);
                        writer.Write(val);
                    }

                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Writes out the Microsoft Office dictionary file, containing all words unique to magic.
        /// </summary>
        private void WriteDictionaryFile()
        {
            Console.Write("Loading dictionary file... ");
            HashSet<string> realWordSet = new HashSet<string>();
            using (StreamReader dictionaryReader = new StreamReader(Settings.Default.InputDictionaryFilename))
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
            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                foreach (Card card in set.Cards)
                {
                    string[] chunks = card.Name.Split(' ');
                    foreach (string chunk in chunks)
                    {
                        string newChunk = chunk.Replace(",", string.Empty).Replace(":", string.Empty);
                        if (!realWordSet.Contains(newChunk.ToLower()))
                        {
                            names.Add(newChunk);
                        }
                    }
                }
            }

            Console.WriteLine("Done");

            Console.Write("Writing dictionary to file... ");
            using (StreamWriter dictionaryOut = new StreamWriter(Settings.Default.OutputDictionaryFilename, false, Encoding.Unicode))
            {
                names.ToList().ForEach(x => dictionaryOut.WriteLine(x));
            }

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Loads the file that contains the list of sets to skip and stores it in a list. wasdfoobar
        /// </summary>
        /// <returns>The list of sets to skip.</returns>
        private List<string> LoadSetsToSkip()
        {
            var setsToSkip = new List<string>();
            StreamReader reader = new StreamReader("data/sets_to_skip.txt");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string setcode = line.Split('#')[0].Trim();
                setsToSkip.Add(setcode);
            }

            return setsToSkip;
        }

        /// <summary>
        /// Parses the additional block file, and stores it in a 
        /// </summary>
        /// <returns>The list of additional blocks to skip.</returns>
        private Dictionary<string, List<string>> LoadAdditionalBlocks()
        {
            var additionalBlocks = new Dictionary<string, List<string>>();
            StreamReader reader = new StreamReader("data/additional_blocks.txt");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] strs = line.Split('\t');
                string blockname = strs[0].Trim();
                List<string> sets = strs[1].Split(' ').ToList();
                sets.ForEach(x => x = x.Trim());
                additionalBlocks.Add(blockname, sets);
            }

            return additionalBlocks;
        }

        /// <summary>
        /// Updates the usercard IDs in the given table name with new IDs, using the oldtonewcards table.
        /// </summary>
        /// <param name="tablename">The table to update.</param>
        /// <param name="command">The command to update the table with.</param>
        private void UpdateTableWithNewCardIDs(string tablename, DbCommand command)
        {
            command.CommandText = $"UPDATE {tablename} t SET cardid = ( SELECT o.newcardid FROM oldtonewcards o WHERE o.oldcardid = t.cardid )";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads the contents of a local file into the given table.
        /// </summary>
        /// <param name="tablename">The name of the table to load the data into.</param>
        /// <param name="filename">The name of the local file to load the data from.</param>
        /// <param name="command">The MySQL command to perform the update with.</param>
        /// <param name="columns">The comma separated list of columns to load the data into.</param>
        private void LoadLocalFileIntoTable(string tablename, string filename, DbCommand command, string columns)
        {
            Console.WriteLine($"Truncating {tablename}");
            command.CommandText = $"TRUNCATE {tablename}";
            command.ExecuteNonQuery();

            Console.WriteLine($"Loading {tablename} table into database.");

            if (command is MySqlCommand)
            {
                command.CommandText = $"LOAD DATA LOCAL INFILE '{filename}' INTO TABLE {tablename} LINES TERMINATED BY '\r\n'";
            }
            else if (command is NpgsqlCommand)
            {
                string winFilename = new FileInfo(filename).FullName;
                command.CommandText = $"COPY {tablename} ({columns}) FROM '{winFilename}' (DELIMITER '\t')";
            }
            else
            {
                throw new ArgumentException();
            }

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Finds all the different types and supertypes and writes them to a file.
        /// </summary>
        private void WriteTypesToFile()
        {
            HashSet<string> typeSet = new HashSet<string>();

            // Ignore Unglued and Unhinged (they have some weird types)
            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(
                x => !this.setsToSkip.Contains(x.Value.Code)
             && x.Value.Code != "UGL" && x.Value.Code != "UNH"))
            {
                Set set = pair.Value;

                foreach (Card card in set.Cards)
                {
                    card.Types?.ForEach(x => typeSet.Add(x));
                    card.Supertypes?.ForEach(x => typeSet.Add(x));
                }
            }

            Console.WriteLine("Writing types table file.");
            StreamWriter sw = new StreamWriter("temp/types", false, this.defaultFileEncoding);

            int typeId = 1;
            foreach (string type in typeSet)
            {
                sw.WriteLine($"{typeId}\t{type}");
                ++typeId;
            }

            sw.Close();
        }

        /// <summary>
        /// Writes all unique subtypes out to a load file.
        /// </summary>
        private void WriteSubtypesToFile()
        {
            HashSet<string> subtypeSet = new HashSet<string>();
            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                foreach (Card card in set.Cards)
                {
                    card.SubTypes?.ForEach(x => subtypeSet.Add(x));
                }
            }

            Console.WriteLine("Writing subtypes table file.");
            StreamWriter sw = new StreamWriter("temp/subtypes", false, this.defaultFileEncoding);

            int subtypeId = 1;
            foreach (string subtype in subtypeSet)
            {
                sw.WriteLine($"{subtypeId}\t{subtype}");
                ++subtypeId;
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

            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

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
                        wc.DownloadFile(url, filename);
                        Console.WriteLine($"Downloaded {filename}");
                    }
                    catch (WebException)
                    {
                        // Swallow
                    }
                }
            }
        }

        /// <summary>
        /// Creates backup files for both the new and old databases
        /// </summary>
        /// <param name="backupMagicDb">Whether to backup magic_db</param>
        /// <param name="backupDelverDb">Whether to backup the delverdb</param>
        /// <param name="backupTutelageDb">Whether to backup tutelage</param>
        private void CreateDatabaseBackups(bool backupMagicDb, bool backupDelverDb, bool backupTutelageDb)
        {
            if (!backupMagicDb && !backupDelverDb && !backupTutelageDb)
            {
                return;
            }

            Console.Write("Creating backup files... ");

            DirectoryInfo backupDir = Directory.CreateDirectory(Path.Combine("backup", string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now)));

            Regex passwordRegex = new Regex("password=(.+?);");
            Match mamysqlMatch = passwordRegex.Match(ConfigurationManager.ConnectionStrings["DelverDbConnection"].ConnectionString);
            string mysqlPassword = mamysqlMatch.Groups[1].Value;

            if (backupMagicDb)
            {
                string oldbackup = $"{backupDir.FullName}\\magic_db.sql";
                Process p = Process.Start("CMD.exe", $"/C mysqldump -u root -p{mysqlPassword} magic_db > \"{oldbackup}\"");
                p.WaitForExit();
            }

            if (backupDelverDb)
            {
                string newbackup = $"{backupDir.FullName}\\delverdb.sql";
                Process p = Process.Start("CMD.exe", $"/C mysqldump -u root -p{mysqlPassword} delverdb > \"{newbackup}\"");
                p.WaitForExit();
            }

            if (backupTutelageDb)
            {
                // Dig the password out of the connection file, then use it to backup the postgres database
                Regex postgresRegex = new Regex("Password=(.+?);");
                Match match = postgresRegex.Match(ConfigurationManager.ConnectionStrings["PostgresConnection"].ConnectionString);
                string password = match.Groups[1].Value;

                // pg_dump can't take a password as a parameter, but it can use an environment variable
                Environment.SetEnvironmentVariable("PGPASSWORD", password);
                string postgresbackup = $"{backupDir.FullName}\\tutelage.sql";
                Process p = Process.Start("pg_dump.exe", $"-U postgres -d tutelage -f {postgresbackup}");
                p.WaitForExit();
            }

            // Zip the directory and delete it
            ZipFile.CreateFromDirectory(backupDir.FullName, $"backup\\{backupDir.Name}.zip");
            backupDir.Delete(true);

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Downloads the images for every card that does not already have one.
        /// </summary>
        private void DownloadCardImages()
        {
            Console.WriteLine("Downloading card images.");

            WebClient wc = new WebClient();
            DirectoryInfo imageDir = new DirectoryInfo(@"C:\wamp\www\delverdb\images\cards");

            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
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
                        Console.WriteLine($"Failed to download {url} {e}");
                    }
                }
            }

            Console.WriteLine("Finished downloading card images.");
        }

        /// <summary>
        /// Sets the a unique Oracle IDs for each card. Cards that are reprinted are given the same Oracle ID for each printing
        /// </summary>
        private void SetOracleIDs()
        {
            Console.Write("Setting oracle IDs... ");

            this.uniqueCards = new Dictionary<string, Card>();
            int oracleID = 0;
            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                foreach (Card card in set.Cards)
                {
                    Card existingCard;
                    if (!this.uniqueCards.TryGetValue(card.Name, out existingCard))
                    {
                        card.OracleID = ++oracleID;
                        this.uniqueCards.Add(card.Name, card);
                    }
                    else
                    {
                        card.OracleID = existingCard.OracleID;
                    }
                }
            }

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Reads the JSON data and stores it in the set dictionary
        /// </summary>
        /// <returns>The set dictionary.</returns>
        private Dictionary<string, Set> ReadJsonData()
        {
            Console.Write("Reading set data...");
            string data = File.ReadAllText(Path.Combine(Settings.Default.JsonDirectory, Settings.Default.JsonFilename));
            Console.WriteLine("Done");

            Console.Write("Deserialising set data... ");
            var setDictionary = JsonConvert.DeserializeObject<Dictionary<string, Set>>(data);
            Console.WriteLine("Done");
            return setDictionary;
        }

        /// <summary>
        /// Writes all card links out to a tab separated file
        /// </summary>
        private void WriteCardLinksTable()
        {
            Console.Write("Writing cards table file...");

            StreamWriter sw = new StreamWriter("temp/links", false, this.defaultFileEncoding);
            foreach (KeyValuePair<string, Card> pair in this.uniqueCards.Where(x => x.Value.Names != null))
            {
                Card card = pair.Value;

                foreach (string linkName in card.Names.FindAll(x => x != card.Name))
                {
                    Card other = this.uniqueCards[linkName];
                    sw.WriteLine($"{card.OracleID}\t{other.OracleID}\t{card.LinkType}");
                }
            }

            sw.Close();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// WRites all oracle card information to a tab separated file.
        /// </summary>
        private void WriteCardsTable()
        {
            Console.Write("Writing cards table file... ");

            StreamWriter sw = new StreamWriter("temp/cards", false, this.defaultFileEncoding);
            foreach (KeyValuePair<string, Card> pair in this.uniqueCards)
            {
                Card card = pair.Value;
                sw.WriteLine(DataSerialiser.SerialiseCard(card));
            }

            sw.Close();
            Console.Write("Done");
        }

        /// <summary>
        /// Writes the card set information to a tab separated file.
        /// </summary>
        /// <param name="includeIdColumn">Whether to include the ID column in the output data.</param>
        private void WriteCardSetsFile(bool includeIdColumn)
        {
            Console.Write("Writing cardsets table file... ");
            StreamWriter sw = new StreamWriter("temp/cardsets", false, this.defaultFileEncoding);

            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                foreach (Card card in set.Cards)
                {
                    sw.WriteLine(DataSerialiser.SerialiseCardSet(card, set, includeIdColumn));
                }
            }

            sw.Close();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Writes the list of blocks out to a tab separated file.
        /// </summary>
        private void WriteBlocksToFile()
        {
            Console.Write("Writing cardsets table file... ");
            StreamWriter sw = new StreamWriter("temp/blocks", false, this.defaultFileEncoding);

            int blockId = 1;
            this.blockMap = new Dictionary<string, int>();

            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                if (set.Block == null || this.blockMap.ContainsKey(set.Block))
                {
                    continue;
                }

                this.blockMap.Add(set.Block, blockId);
                ++blockId;
            }

            // Add the user defined blocks as well
            foreach (KeyValuePair<string, List<string>> pair in this.additionalBlocks)
            {
                this.blockMap.Add(pair.Key, blockId);
                ++blockId;
            }

            // Write to file
            foreach (KeyValuePair<string, int> pair in this.blockMap)
            {
                sw.WriteLine($"{pair.Value}\t{pair.Key}");
            }

            sw.Close();
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Writes the set list out to a loading file.
        /// </summary>
        private void WriteSetsToFile()
        {
            Console.Write("Writing cardsets table file... ");
            StreamWriter sw = new StreamWriter("temp/sets", false, this.defaultFileEncoding);

            int setId = 1;
            foreach (KeyValuePair<string, Set> pair in this.setDictionary.Where(x => !this.setsToSkip.Contains(x.Value.Code)))
            {
                Set set = pair.Value;

                List<string> parts = new List<string>();
                parts.Add(setId.ToString());
                ++setId;

                parts.Add(set.Code);
                parts.Add(set.Name);

                if (string.IsNullOrWhiteSpace(set.Block))
                {
                    // If the set doesn't have a block, try and find it in the additional block list
                    var additionalBlockPair = this.additionalBlocks.FirstOrDefault(x => x.Value.Contains(set.Code));

                    // Key can be null
                    parts.Add(additionalBlockPair.Key);
                }
                else
                {
                    parts.Add(this.blockMap[set.Block].ToString());
                }

                parts.Add(set.ReleaseDate.ToString());

                sw.WriteLine(DataSerialiser.JoinObjectParts(parts));
            }

            sw.Close();
        }
    }
}
