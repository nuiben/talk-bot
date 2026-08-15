using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace talk
{
    // Thrown when a config file exists but cannot be understood, so the caller
    // can say what is wrong with it rather than starting from the defaults and
    // quietly writing over settings somebody chose.
    class SettingsStoreException : Exception
    {
        public SettingsStoreException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }

    // What the user prefers, kept in its own file rather than with the
    // phrases: a phrase is something they wrote and a preference is something
    // they set, and the two are worth losing separately. A file of phrases that
    // will not parse should not also cost somebody their voice.
    //
    // Voice settings are what there is to remember so far. The file is a flat
    // object with one name per setting, which is what makes room for the rest -
    // an API key, a download folder - without the shape having to change.
    static class SettingsStore
    {
        public const int Version = 1;

        public static string DefaultPath
        {
            get { return UserData.PathTo("config.json"); }
        }

        // UTF-8 with no byte order mark: a file meant to be opened in an editor
        // should not start with three bytes that are not JSON.
        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        private static readonly JsonSerializerOptions WriteOptions =
            new JsonSerializerOptions { WriteIndented = true };

        public static string Serialize(VoiceSettings settings, string engine)
        {
            ConfigFile file = new ConfigFile();
            file.Version = Version;
            file.Engine = engine;
            file.Voice = settings.Voice;
            file.Speed = settings.Speed;
            file.Pitch = settings.Pitch;
            file.Volume = settings.Volume;
            return JsonSerializer.Serialize(file, WriteOptions);
        }

        // Read onto the settings that are already there rather than into new
        // ones, because the engine and the settings are both shared and already
        // in use by the time this runs.
        //
        // A setting the file does not mention is left at what it was, which is
        // what makes a half written file - one somebody edited to change the
        // speed and nothing else - a sensible thing to have. Numbers out of
        // range are clamped by the settings themselves, so a hand edited speed
        // of 500 is the fastest rather than a synthesizer error.
        public static void Apply(string json, VoiceSettings settings)
        {
            if (json == null || json.Trim().Length == 0)
            {
                return;
            }

            ConfigFile file;
            try
            {
                file = JsonSerializer.Deserialize<ConfigFile>(json);
            }
            catch (JsonException e)
            {
                throw new SettingsStoreException("the file is not readable JSON", e);
            }
            if (file == null)
            {
                return;
            }

            // The engine goes on first: selecting one drops the voice, since a
            // Kokoro voice name means nothing to espeak, and doing it the other
            // way round would drop the voice this file just set.
            if (file.Engine != null)
            {
                SpeechEngine.Select(file.Engine);
            }
            if (file.Voice != null)
            {
                settings.Voice = file.Voice;
            }
            if (file.Speed.HasValue)
            {
                settings.Speed = file.Speed.Value;
            }
            if (file.Pitch.HasValue)
            {
                settings.Pitch = file.Pitch.Value;
            }
            if (file.Volume.HasValue)
            {
                settings.Volume = file.Volume.Value;
            }
        }

        public static void Load()
        {
            Load(DefaultPath, VoiceSettings.Current);
        }

        // No file is the first run, which is not worth reporting: it is what
        // every user has once, and the defaults are what it would have said.
        public static void Load(string path, VoiceSettings settings)
        {
            if (!File.Exists(path))
            {
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(path, FileEncoding);
            }
            catch (IOException e)
            {
                throw new SettingsStoreException("the file could not be read: " + e.Message, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new SettingsStoreException("the file could not be read: " + e.Message, e);
            }
            Apply(json, settings);
        }

        public static void Save()
        {
            Save(DefaultPath, VoiceSettings.Current, SpeechEngine.Selected);
        }

        // Written beside the real file and moved onto it, so a save that is
        // interrupted leaves the settings that were already there rather than
        // half of them.
        public static void Save(string path, VoiceSettings settings, string engine)
        {
            string json = Serialize(settings, engine);
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
                throw new SettingsStoreException("the file could not be written: " + e.Message, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new SettingsStoreException("the file could not be written: " + e.Message, e);
            }
        }

        // Every setting is nullable so that missing and set-to-zero can be told
        // apart: a volume of 0 is silence somebody chose, and a file with no
        // volume in it is one that has nothing to say about volume. A voice of
        // null is the synthesizer's own, which is why it is written out rather
        // than left off.
        private class ConfigFile
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("engine")]
            public string Engine { get; set; }

            [JsonPropertyName("voice")]
            public string Voice { get; set; }

            [JsonPropertyName("speed")]
            public int? Speed { get; set; }

            [JsonPropertyName("pitch")]
            public int? Pitch { get; set; }

            [JsonPropertyName("volume")]
            public int? Volume { get; set; }
        }
    }
}
