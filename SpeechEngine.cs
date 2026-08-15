using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace talk
{
    // System.Speech only exists on Windows, so speech is abstracted here and
    // each platform gets its own backend.
    interface ISpeechEngine
    {
        // Returns when the phrase has been read, or sooner if it was stopped.
        // Reads VoiceSettings.Current as it starts, so a change made in the
        // settings menu applies to the next phrase without anything being
        // rebuilt.
        void Speak(string phrase);

        // Cuts the current phrase short. Safe to call when nothing is playing.
        void Stop();

        // Names this backend accepts for VoiceSettings.Voice. Empty when it
        // only has the one voice, so the menu can say so rather than offering
        // an empty list.
        string[] AvailableVoices();

        // One line for the settings screen: which backend is speaking and
        // which of the dials it actually honours.
        string Describe();
    }

    // Speaking blocks the menu, so the only way to cut a whole web page short
    // is to watch the keyboard while it plays.
    static class SpeechInterrupt
    {
        // True when the listener stopped it, false when it finished on its own.
        // Console.KeyAvailable throws when input is redirected, as it is under
        // piped input or a test run, so those hosts just wait for the end.
        public static bool WaitFor(Func<bool> finished, Action stop)
        {
            bool watching = !Console.IsInputRedirected;
            if (watching)
            {
                // Anything typed before the phrase started belongs to the menu,
                // not to this prompt, or the first keystroke would stop speech
                // that had not begun yet.
                Drain();
                Console.WriteLine("  (speaking - press any key to stop)");
            }

            while (!finished())
            {
                if (watching && Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    stop();
                    return true;
                }
                Thread.Sleep(100);
            }
            return false;
        }

        private static void Drain()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }
    }

    static class SpeechEngine
    {
        private static ISpeechEngine instance;

        public static ISpeechEngine Current
        {
            get
            {
                if (instance == null)
                {
                    instance = Create();
                }
                return instance;
            }
        }

        private static ISpeechEngine Create()
        {
#if WINDOWS
            return new WindowsSpeechEngine();
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new CommandSpeechEngine("say");
            }
            return CommandSpeechEngine.FindLinuxEngine();
#endif
        }
    }

    // Shells out to a command line synthesizer. The phrase is passed as a
    // separate argument, so it is never interpreted by a shell.
    class CommandSpeechEngine : ISpeechEngine
    {
        private static readonly string[] LinuxCandidates =
        {
            "spd-say", "espeak-ng", "espeak", "festival"
        };

        private readonly string command;

        public CommandSpeechEngine(string newCommand)
        {
            command = newCommand;
        }

        public static ISpeechEngine FindLinuxEngine()
        {
            foreach (string candidate in LinuxCandidates)
            {
                if (Exists(candidate))
                {
                    return new CommandSpeechEngine(candidate);
                }
            }
            return new NullSpeechEngine(
                "No speech synthesizer found. Install one, for example: sudo pacman -S espeak-ng");
        }

        private static bool Exists(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (string dir in path.Split(Path.PathSeparator))
            {
                if (dir.Length > 0 && File.Exists(Path.Combine(dir, name)))
                {
                    return true;
                }
            }
            return false;
        }

        // Held so Stop can reach the synthesizer that is running now. Speak is
        // only ever called from the menu loop, but Stop is called from the key
        // listener, so the field is written and read as one operation.
        private Process speaking;

        public void Speak(string phrase)
        {
            string spoken = phrase == null ? "" : phrase;
            ProcessStartInfo info = new ProcessStartInfo(command);
            foreach (string argument in Arguments(command, spoken, VoiceSettings.Current))
            {
                info.ArgumentList.Add(argument);
            }
            info.RedirectStandardInput = command == "festival";

            using (Process p = Process.Start(info))
            {
                Interlocked.Exchange(ref speaking, p);
                try
                {
                    if (info.RedirectStandardInput)
                    {
                        p.StandardInput.Write(spoken);
                        p.StandardInput.Close();
                    }
                    SpeechInterrupt.WaitFor(delegate { return p.HasExited; }, Stop);
                    p.WaitForExit();
                }
                finally
                {
                    Interlocked.Exchange(ref speaking, null);
                }
            }
        }

        // Killing the whole tree matters for festival and spd-say, which speak
        // through a helper process rather than themselves.
        public void Stop()
        {
            Process p = Interlocked.CompareExchange(ref speaking, null, null);
            if (p == null)
            {
                return;
            }

            // The process can finish between the check and the kill, which is
            // not a failure: the phrase is over either way.
            try
            {
                if (!p.HasExited)
                {
                    p.Kill(true);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        // The whole command line for one phrase, kept as a function of its
        // inputs so a test can read it without a synthesizer being installed.
        //
        // The phrase goes last, behind "--", because a phrase can begin with a
        // dash: "-v is the voice flag" typed as a phrase was read as options by
        // every one of these commands, which then spoke nothing at all. Every
        // backend here parses with getopt, so every one of them stops reading
        // options at "--".
        public static List<string> Arguments(string command, string phrase,
            VoiceSettings voice)
        {
            List<string> arguments = new List<string>();
            if (command == "festival")
            {
                // festival takes the phrase on its standard input rather than
                // as an argument, and has no dials to set.
                arguments.Add("--tts");
                return arguments;
            }

            AddVoiceArguments(command, arguments, voice);
            arguments.Add("--");
            arguments.Add(phrase == null ? "" : phrase);
            return arguments;
        }

        // Each synthesizer names and scales its dials differently, so the
        // settings are translated here rather than being stored in any one
        // backend's units.
        private static void AddVoiceArguments(string command, List<string> arguments,
            VoiceSettings voice)
        {
            if (command == "say")
            {
                // Speed is the only dial the macOS voices take on the command
                // line; pitch and volume belong to the voice itself there.
                if (voice.Voice != null)
                {
                    arguments.Add("-v");
                    arguments.Add(voice.Voice);
                }
                arguments.Add("-r");
                arguments.Add(VoiceSettings.Scale(voice.Speed, 90, 175, 350).ToString());
                return;
            }

            if (command == "spd-say")
            {
                // speech-dispatcher takes a voice type rather than a name, and
                // every dial on a -100..100 scale centred on its normal.
                if (voice.Voice != null)
                {
                    arguments.Add("-t");
                    arguments.Add(voice.Voice);
                }
                arguments.Add("-r");
                arguments.Add((voice.Speed * 10).ToString());
                arguments.Add("-p");
                arguments.Add((voice.Pitch * 10).ToString());
                arguments.Add("-i");
                arguments.Add((voice.Volume * 2 - 100).ToString());
                // Without this spd-say returns as soon as the phrase has been
                // handed to the daemon, which would end the phrase instantly.
                arguments.Add("-w");
                return;
            }

            // espeak and espeak-ng, which take words a minute, a 0-99 pitch and
            // an amplitude whose default is 100.
            if (voice.Voice != null)
            {
                arguments.Add("-v");
                arguments.Add(voice.Voice);
            }
            arguments.Add("-s");
            arguments.Add(VoiceSettings.Scale(voice.Speed, 80, 175, 450).ToString());
            arguments.Add("-p");
            arguments.Add(VoiceSettings.Scale(voice.Pitch, 0, 50, 99).ToString());
            arguments.Add("-a");
            arguments.Add(voice.Volume.ToString());
        }

        public string Describe()
        {
            if (command == "festival")
            {
                return "festival - reads the text as it is, with no voice options";
            }
            if (command == "say")
            {
                return "say - voice and speed (pitch and volume come with the voice)";
            }
            if (command == "spd-say")
            {
                return "spd-say - voice, speed, pitch and volume";
            }
            return command + " - voice, speed, pitch and volume";
        }

        // speech-dispatcher speaks through whichever module is configured, so
        // it offers a handful of voice types rather than named voices.
        private static readonly string[] SpdVoiceTypes =
        {
            "MALE1", "MALE2", "MALE3", "FEMALE1", "FEMALE2", "FEMALE3",
            "CHILD_MALE", "CHILD_FEMALE"
        };

        // espeak ships a variant for each voice, which changes its character
        // far more than pitch alone does, so they are offered alongside the
        // language voices.
        private static readonly string[] EspeakVariants =
        {
            "m1", "m2", "m3", "m4", "m5", "m6", "m7",
            "f1", "f2", "f3", "f4", "f5", "croak", "whisper"
        };

        public string[] AvailableVoices()
        {
            if (command == "festival")
            {
                return new string[0];
            }
            if (command == "spd-say")
            {
                return SpdVoiceTypes;
            }
            if (command == "say")
            {
                return MacVoices();
            }
            return EspeakVoices();
        }

        // "say -v ?" lists a voice, its language and a sample line, separated
        // by runs of spaces. Voice names contain spaces of their own ("Bad
        // News"), so the name is everything before the first double space.
        private string[] MacVoices()
        {
            List<string> voices = new List<string>();
            foreach (string line in Capture("-v", "?"))
            {
                int gap = line.IndexOf("  ", StringComparison.Ordinal);
                if (gap > 0)
                {
                    voices.Add(line.Substring(0, gap).Trim());
                }
            }
            return voices.ToArray();
        }

        // espeak knows a couple of hundred languages, which is more than anyone
        // wants to walk through, so the list is the English ones plus the
        // variants of the default voice.
        private string[] EspeakVoices()
        {
            List<string> voices = new List<string>();
            foreach (string line in Capture("--voices"))
            {
                // Pty Language Age/Gender VoiceName File Other
                string[] columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length < 4 || columns[1] == "Language")
                {
                    continue;
                }
                if (columns[1].StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    voices.Add(columns[1]);
                }
            }

            foreach (string variant in EspeakVariants)
            {
                voices.Add("en+" + variant);
            }
            return voices.ToArray();
        }

        // Asking a synthesizer what it can do should never stop the menu, so a
        // backend that will not answer is treated as having nothing to offer.
        private string[] Capture(params string[] arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(command);
                foreach (string argument in arguments)
                {
                    info.ArgumentList.Add(argument);
                }
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (Process p = Process.Start(info))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch (Exception)
            {
                return new string[0];
            }
        }
    }

    // Used when the platform has no synthesizer available, so the menu still
    // works instead of crashing.
    class NullSpeechEngine : ISpeechEngine
    {
        private readonly string reason;

        public NullSpeechEngine(string newReason)
        {
            reason = newReason;
        }

        public void Speak(string phrase)
        {
            Console.WriteLine("[silent] " + phrase);
            Console.WriteLine(reason);
        }

        // Nothing is ever playing, so there is nothing to stop.
        public void Stop()
        {
        }

        public string[] AvailableVoices()
        {
            return new string[0];
        }

        public string Describe()
        {
            return reason;
        }
    }
}
