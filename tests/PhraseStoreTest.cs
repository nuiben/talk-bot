using System;
using System.IO;
using System.Text;

namespace talk.Tests
{
    // Phrases on disk. The cases here are the ones a user reaches by their
    // first run, by saving a page full of newlines and accents, and by a file
    // that has been edited by hand or written over by something else.
    internal class PhraseStoreTest : ITest
    {
        public string Name
        {
            get { return "phrase store"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            // What a phrase is made of has to survive the trip out and back:
            // a fetched page is thousands of characters of newlines, quotes
            // and whatever script the page was written in.
            Phrase[] written = new Phrase[]
            {
                new Phrase(1, "hello"),
                new Phrase(2, "a heading\nand a \"quoted\" line\nwith an accent: café"),
                new Phrase(7, "pengy")
            };
            Phrase[] read = PhraseStore.Deserialize(PhraseStore.Serialize(written));
            checks.Equal("every phrase comes back", 3, read.Length);
            checks.Equal("with its ID", 7, read[2].GetId());
            checks.Equal("and its text exactly", written[1].GetPhrase(), read[1].GetPhrase());

            // The first run has no file, and an empty library is written as one
            // rather than as nothing at all.
            checks.Equal("nothing on disk reads as no phrases", 0,
                PhraseStore.Deserialize("").Length);
            checks.Equal("an empty library round trips", 0,
                PhraseStore.Deserialize(PhraseStore.Serialize(new Phrase[0])).Length);

            // Written for a person to read and fix, so the names in it are part
            // of the format rather than whatever the serializer felt like.
            string json = PhraseStore.Serialize(written);
            checks.Equal("the file says which version it is", true,
                json.Contains("\"version\": 1"));
            checks.Equal("phrases are named in the file", true, json.Contains("\"phrases\""));
            checks.Equal("and so are their fields", true,
                json.Contains("\"id\"") && json.Contains("\"text\""));

            // A file edited by hand is the one that arrives malformed, and the
            // program has to say so rather than start again from empty and
            // write over what was there.
            bool reported = false;
            try
            {
                PhraseStore.Deserialize("{ \"phrases\": [ ");
            }
            catch (PhraseStoreException)
            {
                reported = true;
            }
            checks.Equal("a broken file is reported", true, reported);

            // Entries the program would not have written are left out: an empty
            // phrase is what the add prompt already turns away, and a repeated
            // ID would give the remove menu two rows meaning the same thing.
            Phrase[] odd = PhraseStore.Deserialize(
                "{\"version\":1,\"phrases\":[" +
                "{\"id\":1,\"text\":\"kept\"}," +
                "{\"id\":2,\"text\":\"   \"}," +
                "{\"id\":3}," +
                "{\"id\":1,\"text\":\"a second phrase one\"}]}");
            checks.Equal("only the phrase worth keeping is kept", 1, odd.Length);
            checks.Equal("and it is the first one", "kept", odd[0].GetPhrase());

            // Phrases read back start the library, so they keep their IDs and
            // the next one typed carries on past the highest of them instead of
            // colliding with a phrase that is already saved.
            PhraseLibrary library = new PhraseLibrary(read);
            checks.Equal("saved phrases start the library", 3, library.ListPhrases().Length);
            checks.Equal("the next ID follows the highest saved one", 8, library.TakeNextId());
            checks.Equal("and the one after that is not the same", 9, library.TakeNextId());

            // Removing the last phrase used to free its ID for the next one,
            // which meant two different phrases could be saved under one ID in
            // the same file.
            library.RemovePhrase(7);
            checks.Equal("an ID freed by a removal is not handed out again", 10,
                library.TakeNextId());

            checks.Equal("a fresh library starts at one", 1,
                new PhraseLibrary().TakeNextId());

            return RunFileChecks(checks);
        }

        // The file itself, written and read where the tests run rather than in
        // the user's data folder, so a test run never touches phrases somebody
        // actually saved.
        private bool RunFileChecks(Checks checks)
        {
            string path = Path.Combine(Path.GetTempPath(),
                "talk-bot-test-" + Guid.NewGuid().ToString("N"), "phrases.json");
            try
            {
                // A missing file is the first run: no phrases, and no complaint.
                checks.Equal("a missing file reads as no phrases", 0,
                    PhraseStore.Load(path).Length);

                // The folder it goes in does not exist yet either, on the run
                // that saves the first phrase.
                PhraseStore.Save(path, new Phrase[] { new Phrase(4, "saved to disk") });
                checks.Equal("the file was written", true, File.Exists(path));

                Phrase[] loaded = PhraseStore.Load(path);
                checks.Equal("and reads back as one phrase", 1, loaded.Length);
                checks.Equal("with its ID", 4, loaded[0].GetId());
                checks.Equal("and its text", "saved to disk", loaded[0].GetPhrase());

                // Every save writes the whole list, so a removal has to leave
                // the file shorter rather than only adding to it.
                PhraseStore.Save(path, new Phrase[0]);
                checks.Equal("a removal is written too", 0, PhraseStore.Load(path).Length);

                // The half written file a save is interrupted partway through
                // would look like this one.
                File.WriteAllText(path, "{ \"version\": 1, \"phrases\": [", Encoding.UTF8);
                bool reported = false;
                try
                {
                    PhraseStore.Load(path);
                }
                catch (PhraseStoreException)
                {
                    reported = true;
                }
                checks.Equal("a broken file on disk is reported", true, reported);
            }
            finally
            {
                string directory = Path.GetDirectoryName(path);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }

            return checks.Report(Name, "phrases survive the program, and a bad file is reported");
        }
    }
}
