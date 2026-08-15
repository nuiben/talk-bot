using System;
using System.IO;

namespace talk
{
    // Where the bot keeps what it has to remember between runs: the phrases,
    // the settings, and the Kokoro model that is too large to fetch twice.
    //
    // Not the working directory, which is wherever the bot happened to be
    // started from - a phrase saved from the source tree would be missing the
    // next time it was run from somewhere else, and the model would be
    // downloaded again at 320MB a time.
    internal static class UserData
    {
        public static string Folder
        {
            get
            {
                string data = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(data))
                {
                    // A service or a stripped down account can have no such
                    // folder, and the executable's own is better than nothing
                    // at all: it at least outlives the run.
                    data = AppContext.BaseDirectory;
                }
                return Path.Combine(data, "talk-bot");
            }
        }

        public static string PathTo(string fileName)
        {
            return Path.Combine(Folder, fileName);
        }
    }
}
