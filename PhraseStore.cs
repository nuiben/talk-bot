using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace talk
{
    // Thrown when a phrase file exists but cannot be understood, so the caller
    // can say what is wrong with the file instead of starting again from an
    // empty list and quietly writing over it.
    class PhraseStoreException : Exception
    {
        public PhraseStoreException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }

    // Phrases as text on disk, so a saved page or a typed line outlives the
    // program that made it.
    //
    // The JSON is written from a record of its own rather than from Phrase
    // itself. Phrase keeps its fields private and gained a Play method along
    // the way, and neither of those is anything the file should have an opinion
    // about; a shape written out on purpose is also the shape a database table
    // is made from later, one column per property.
    //
    // The file carries a version so that a later change to it - a voice per
    // phrase is the one already on the roadmap - can be told apart from this
    // one by whatever reads it then, rather than guessed at.
    static class PhraseStore
    {
        public const int Version = 1;

        public static string DefaultPath
        {
            get { return UserData.PathTo("phrases.json"); }
        }

        // UTF-8 without the byte order mark Encoding.UTF8 writes. Our own read
        // strips one, but a file meant to be opened in an editor or handed to
        // another parser should not start with three bytes that are not JSON.
        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        private static readonly JsonSerializerOptions WriteOptions =
            new JsonSerializerOptions
            {
                // A phrase is something a person typed, so the file is worth
                // being able to read and fix by hand.
                WriteIndented = true
            };

        public static string Serialize(Phrase[] phrases)
        {
            List<PhraseRecord> records = new List<PhraseRecord>();
            if (phrases != null)
            {
                foreach (Phrase p in phrases)
                {
                    if (p == null)
                    {
                        continue;
                    }
                    PhraseRecord record = new PhraseRecord();
                    record.Id = p.GetId();
                    record.Text = p.GetPhrase() == null ? "" : p.GetPhrase();
                    records.Add(record);
                }
            }

            PhraseFile file = new PhraseFile();
            file.Version = Version;
            file.Phrases = records;
            return JsonSerializer.Serialize(file, WriteOptions);
        }

        // Anything the program would not have written is left out rather than
        // loaded: an entry with no text is the empty phrase that the add prompt
        // already turns away, and a repeated ID would give the remove and speak
        // menus two rows that mean the same thing. Text that is merely
        // unexpected - a newline, an accent, a page in another script - is
        // kept, because a fetched page is full of it.
        public static Phrase[] Deserialize(string json)
        {
            if (json == null || json.Trim().Length == 0)
            {
                return new Phrase[0];
            }

            PhraseFile file;
            try
            {
                file = JsonSerializer.Deserialize<PhraseFile>(json);
            }
            catch (JsonException e)
            {
                throw new PhraseStoreException("the file is not readable JSON", e);
            }

            if (file == null || file.Phrases == null)
            {
                return new Phrase[0];
            }

            List<Phrase> phrases = new List<Phrase>();
            HashSet<int> seen = new HashSet<int>();
            foreach (PhraseRecord record in file.Phrases)
            {
                if (record == null || record.Text == null || record.Text.Trim().Length == 0)
                {
                    continue;
                }
                if (!seen.Add(record.Id))
                {
                    continue;
                }
                phrases.Add(new Phrase(record.Id, record.Text));
            }
            return phrases.ToArray();
        }

        public static Phrase[] Load()
        {
            return Load(DefaultPath);
        }

        // No file is the first run, which is not a problem worth reporting: it
        // is what every user has once.
        public static Phrase[] Load(string path)
        {
            if (!File.Exists(path))
            {
                return new Phrase[0];
            }

            string json;
            try
            {
                json = File.ReadAllText(path, FileEncoding);
            }
            catch (IOException e)
            {
                throw new PhraseStoreException("the file could not be read: " + e.Message, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new PhraseStoreException("the file could not be read: " + e.Message, e);
            }
            return Deserialize(json);
        }

        public static void Save(Phrase[] phrases)
        {
            Save(DefaultPath, phrases);
        }

        // Written beside the real file and moved onto it, so a save that is
        // interrupted - the window closed partway through a page of several
        // thousand characters - leaves the phrases that were already there
        // rather than half of them.
        public static void Save(string path, Phrase[] phrases)
        {
            string json = Serialize(phrases);
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string partial = path + ".partial";
                File.WriteAllText(partial, json, FileEncoding);
                File.Move(partial, path, true);
            }
            catch (IOException e)
            {
                throw new PhraseStoreException("the file could not be written: " + e.Message, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new PhraseStoreException("the file could not be written: " + e.Message, e);
            }
        }

        // The file itself. Public properties because that is what the
        // serializer reads, and named as the JSON names them.
        private class PhraseFile
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("phrases")]
            public List<PhraseRecord> Phrases { get; set; }
        }

        private class PhraseRecord
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }
        }
    }
}
