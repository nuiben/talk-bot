using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;

namespace talk
{
    // Kokoro is a neural synthesizer that runs here in the process rather than
    // being shelled out to, so unlike the other backends it has a model to load
    // before it can say anything. The model is about 320MB and is downloaded on
    // first use, which is why nothing here happens until the first phrase: a
    // user who never picks Kokoro never pays for it.
    //
    // The voices are a different matter. They ship with the package as small
    // files next to the executable, so the list can be offered on the settings
    // screen without the model having been fetched at all.
    class KokoroSpeechEngine : ISpeechEngine
    {
        // Kokoro speaks at a multiplier of its own normal pace. Below about
        // half speed the voices slur and above double they turn to chipmunk, so
        // the dial is mapped inside that.
        private const float Slowest = 0.5f;
        private const float Normal = 1.0f;
        private const float Fastest = 2.0f;

        // The folder the package copies the voice files into, resolved against
        // the executable rather than the working directory so the list is found
        // whichever folder the bot was started from.
        private static string VoicesPath
        {
            get { return Path.Combine(AppContext.BaseDirectory, "voices"); }
        }

        // Left to itself KokoroSharp downloads the model into the working
        // directory, which means it lands wherever the bot happened to be
        // started from: 320MB in the source tree if that was here, and another
        // 320MB the next time it is run from somewhere else. It is kept in the
        // user's own data folder instead, so it is fetched once per machine and
        // survives a clean build.
        private const string ModelFile = "kokoro.onnx";

        private static string ModelDirectory
        {
            get
            {
                string data = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(data))
                {
                    data = AppContext.BaseDirectory;
                }
                return Path.Combine(data, "talk-bot");
            }
        }

        private static string ModelPath
        {
            get { return Path.Combine(ModelDirectory, ModelFile); }
        }

        // Loaded on the first phrase and kept, because loading it is the slow
        // part and every later phrase should be quick.
        private KokoroTTS tts;

        // Why the model could not be loaded, if it could not be. Held so the
        // reason can be said again on the settings screen rather than only once
        // at the phrase that hit it.
        private string failure;

        // The handle for the phrase being spoken now, so Stop can reach it from
        // the key listener while Speak is still blocked.
        private SynthesisHandle speaking;

        public static bool ModelDownloaded()
        {
            try
            {
                return File.Exists(ModelPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Speak(string phrase)
        {
            string spoken = phrase == null ? "" : phrase;
            VoiceSettings settings = VoiceSettings.Current;

            KokoroTTS engine = Load();
            if (engine == null)
            {
                Console.WriteLine("[silent] " + spoken);
                Console.WriteLine(failure);
                return;
            }

            KokoroVoice voice = FindVoice(settings.Voice);
            if (voice == null)
            {
                Console.WriteLine("Kokoro has no voice called " + settings.VoiceName +
                    ", so this was read in " + DefaultVoiceName + ".");
                voice = FindVoice(DefaultVoiceName);
            }

            KokoroTTSPipelineConfig pipeline = new KokoroTTSPipelineConfig();
            pipeline.Speed = Speed(settings.Speed);

            SynthesisHandle handle;
            try
            {
                // Volume belongs to the player rather than to the phrase, so it
                // is set before the phrase starts rather than passed with it.
                // This is also the first thing to touch the audio device, and
                // so the first thing to fail on a machine that has none.
                engine.SetVolume(settings.Volume / 100f);

                // Speech is finished when the player says so, not when
                // inference is.
                handle = engine.SpeakFast(spoken, voice, pipeline);
            }
            catch (DllNotFoundException e)
            {
                failure = NoAudio(e.Message);
                Console.WriteLine("[silent] " + spoken);
                Console.WriteLine(failure);
                return;
            }

            Interlocked.Exchange(ref speaking, handle);
            try
            {
                // Assigned after the call because that is where the handle
                // comes from. Inference on even a short phrase takes long
                // enough that the callbacks cannot have been raised yet, and
                // the job state is checked alongside them so a phrase that
                // somehow beat us here still ends the wait.
                handle.OnSpeechCompleted = delegate { Finished(); };
                handle.OnSpeechCanceled = delegate { Finished(); };

                SpeechInterrupt.WaitFor(delegate { return Done(handle); }, Stop);
            }
            finally
            {
                Interlocked.Exchange(ref speaking, null);
                finished = 0;
            }
        }

        private int finished;

        private void Finished()
        {
            Interlocked.Exchange(ref finished, 1);
        }

        private bool Done(SynthesisHandle handle)
        {
            if (Interlocked.CompareExchange(ref finished, 0, 0) == 1)
            {
                return true;
            }
            // A job that was canceled raises nothing once the playback queue
            // has already been emptied, so its state is the backstop.
            return handle.Job != null && handle.Job.State == KokoroJobState.Canceled;
        }

        public void Stop()
        {
            SynthesisHandle handle = Interlocked.CompareExchange(ref speaking, null, null);
            if (handle == null)
            {
                return;
            }

            try
            {
                if (handle.Job != null)
                {
                    handle.Job.Cancel();
                }
                if (tts != null)
                {
                    tts.StopPlayback();
                }
            }
            catch (Exception)
            {
                // The phrase is over either way, and a player that has already
                // torn itself down should not take the menu with it.
            }
            Finished();
        }

        // The name a phrase is read in when nothing has been picked. af_heart
        // is the one voice hexgrad grades an A, so it is the one to be heard
        // first by anyone who just wants to know what Kokoro sounds like.
        public const string DefaultVoiceName = "af_heart";

        // Every voice the package ships, ordered so the English ones come
        // first: the phrase library and the sample line are both English, so
        // those are what a user is most likely looking for.
        public string[] AvailableVoices()
        {
            List<KokoroVoice> voices = Voices();
            List<string> names = new List<string>();
            foreach (KokoroLanguage language in LanguageOrder)
            {
                foreach (KokoroVoice voice in voices)
                {
                    if (voice.Language == language)
                    {
                        names.Add(voice.Name);
                    }
                }
            }
            return names.ToArray();
        }

        // English first, then the rest in the order Kokoro lists them. Mandarin
        // is last because the v1.1-zh release brought a hundred numbered voices
        // with it, which would otherwise bury everything else.
        private static readonly KokoroLanguage[] LanguageOrder =
        {
            KokoroLanguage.AmericanEnglish, KokoroLanguage.BritishEnglish,
            KokoroLanguage.Spanish, KokoroLanguage.French, KokoroLanguage.Hindi,
            KokoroLanguage.Italian, KokoroLanguage.Japanese,
            KokoroLanguage.BrazilianPortuguese, KokoroLanguage.MandarinChinese
        };

        // Which list a voice belongs on. A hundred and fifty voices is far too
        // many for one menu, so the settings screen asks for a language first
        // and this is what it groups them by.
        public string VoiceGroup(string voice)
        {
            foreach (KokoroVoice candidate in Voices())
            {
                if (candidate.Name == voice)
                {
                    return LanguageName(candidate.Language) + " (" +
                        candidate.Gender.ToString().ToLowerInvariant() + ")";
                }
            }
            return null;
        }

        private static string LanguageName(KokoroLanguage language)
        {
            switch (language)
            {
                case KokoroLanguage.AmericanEnglish: return "American English";
                case KokoroLanguage.BritishEnglish: return "British English";
                case KokoroLanguage.BrazilianPortuguese: return "Brazilian Portuguese";
                case KokoroLanguage.MandarinChinese: return "Mandarin Chinese";
                default: return language.ToString();
            }
        }

        public string Describe()
        {
            if (failure != null)
            {
                return failure;
            }

            // Said before anything has been downloaded, since a machine with
            // nothing to play through will not get any use out of the model.
            string missing;
            if (!AudioAvailable(out missing))
            {
                return NoAudio(missing);
            }

            if (!ModelDownloaded())
            {
                return "kokoro - voice, speed and volume (the model downloads on the first phrase)";
            }
            return "kokoro - voice, speed and volume (the voices carry their own pitch)";
        }

        // Kokoro takes a multiplier rather than words a minute, so the dial is
        // scaled the way VoiceSettings.Scale does it for the others: the two
        // halves separately, keeping 0 on the pace the model was trained at.
        public static float Speed(int dial)
        {
            if (dial == 0)
            {
                return Normal;
            }
            if (dial < 0)
            {
                return Normal - (Normal - Slowest) * -dial / 10f;
            }
            return Normal + (Fastest - Normal) * dial / 10f;
        }

        // The voice files sit next to the executable and are read once. An
        // installation missing them leaves the list empty, which the settings
        // screen already knows how to say.
        private static bool voicesLoaded;

        private static List<KokoroVoice> Voices()
        {
            if (!voicesLoaded)
            {
                voicesLoaded = true;
                try
                {
                    if (Directory.Exists(VoicesPath))
                    {
                        KokoroVoiceManager.LoadVoicesFromPath(VoicesPath);
                    }
                }
                catch (Exception)
                {
                }
            }
            return KokoroVoiceManager.Voices;
        }

        private static KokoroVoice FindVoice(string name)
        {
            if (name == null)
            {
                name = DefaultVoiceName;
            }
            foreach (KokoroVoice voice in Voices())
            {
                if (voice.Name == name)
                {
                    return voice;
                }
            }
            return null;
        }

        // Null when the model could not be loaded, with the reason left in
        // failure. Every way this can go wrong - no network for the download,
        // no audio device to play through - is something the user can fix, so
        // it is reported rather than thrown.
        private KokoroTTS Load()
        {
            if (tts != null)
            {
                return tts;
            }
            if (failure != null)
            {
                return null;
            }

            // Checked before the download rather than after it, because being
            // told there is nothing to play through is worth knowing before
            // 320MB has been fetched rather than once it has.
            string missing;
            if (!AudioAvailable(out missing))
            {
                failure = NoAudio(missing);
                return null;
            }

            try
            {
                // Before the model, because the synthesizer builds its player
                // as it is constructed and reads the hook only then.
                KokoroAudioPlayer.Install();

                if (!ModelDownloaded())
                {
                    Console.WriteLine("Fetching the Kokoro model (about 320MB). This happens once.");
                    Download();
                }
                tts = KokoroTTS.LoadModel(ModelPath);
                return tts;
            }
            catch (DllNotFoundException e)
            {
                failure = NoAudio(e.Message);
                return null;
            }
            catch (Exception e)
            {
                failure = "Kokoro could not start: " + e.Message;
                return null;
            }
        }

        // KokoroSharp plays through whatever the platform gives it, and on
        // Linux that is OpenAL, which the package does not bring with it. The
        // library is loaded the first time the volume is set, which is deep
        // inside the first phrase, so it is looked for up front instead: the
        // alternative is a DllNotFoundException out of the middle of Speak
        // after a 320MB download.
        //
        // Windows goes through WinMM and macOS through its own player, both of
        // which are part of the system, so there is nothing to look for there.
        private static readonly string[] OpenAlNames =
        {
            "libopenal.so.1", "libopenal.so"
        };

        private static bool AudioAvailable(out string missing)
        {
            missing = OpenAlNames[0];
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return true;
            }

            foreach (string name in OpenAlNames)
            {
                IntPtr handle;
                if (NativeLibrary.TryLoad(name, out handle))
                {
                    // Left loaded: KokoroSharp is about to want it anyway, and
                    // freeing it here would only have it opened again.
                    return true;
                }
            }
            return false;
        }

        private static string NoAudio(string detail)
        {
            return "kokoro cannot reach an audio device - " + detail +
                " is missing. On Arch: sudo pacman -S openal";
        }

        // KokoroSharp writes the model into the working directory and gives no
        // say in where, so the working directory is moved to the folder it
        // should land in and put back afterwards. Nothing else in the bot reads
        // it while a phrase is being set up, and it is restored in a finally so
        // a failed download does not leave the process somewhere else.
        //
        // It downloads into a scratch folder first and only moves the file into
        // place once it is whole. A download that is interrupted - the machine
        // sleeping, the user giving up on it - would otherwise leave a
        // part-written kokoro.onnx that looks downloaded from then on, and the
        // next run would fail on a truncated model rather than fetching it
        // again.
        private static void Download()
        {
            string scratch = Path.Combine(ModelDirectory, "partial");
            Directory.CreateDirectory(scratch);

            string was = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = scratch;
                int shown = -1;
                KokoroLoader.DownloadModelAsync(KModel.float32, delegate (float done)
                {
                    // The callback is raised for every block that arrives,
                    // which is far too many lines for a console, so it only
                    // speaks up when another tenth has landed.
                    int tenths = (int)(done * 10);
                    if (tenths > shown)
                    {
                        shown = tenths;
                        Console.WriteLine("  " + tenths * 10 + "%");
                    }
                }).GetAwaiter().GetResult();
            }
            finally
            {
                Environment.CurrentDirectory = was;
            }

            File.Move(Path.Combine(scratch, ModelFile), ModelPath, true);
            try
            {
                Directory.Delete(scratch, true);
            }
            catch (IOException)
            {
                // An empty scratch folder left behind is not worth failing for.
            }
        }
    }
}
