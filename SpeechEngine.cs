using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace talk
{
    // System.Speech only exists on Windows, so speech is abstracted here and
    // each platform gets its own backend.
    interface ISpeechEngine
    {
        void Speak(string phrase);
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

        public void Speak(string phrase)
        {
            ProcessStartInfo info = new ProcessStartInfo(command);
            if (command == "festival")
            {
                info.ArgumentList.Add("--tts");
                info.RedirectStandardInput = true;
            }
            else
            {
                info.ArgumentList.Add(phrase);
            }

            using (Process p = Process.Start(info))
            {
                if (info.RedirectStandardInput)
                {
                    p.StandardInput.Write(phrase);
                    p.StandardInput.Close();
                }
                p.WaitForExit();
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
    }
}
