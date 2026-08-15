using System;
using System.Runtime.InteropServices;

namespace talk
{
    // Silences the browser while it runs.
    //
    // Firefox and geckodriver write to the same two streams this program draws
    // its menu on, and neither can be talked out of it from here: the driver
    // reports the port it read on standard output, and the browser reports
    // every script error on any page it opens to standard error. Selenium's
    // log level covers the driver's own log and the devtools.console.stdout
    // preferences cover the page console, but these lines come from neither,
    // and a page like GitHub produces dozens of them - enough to scroll a menu
    // off the screen mid-fetch.
    //
    // They are the child's writes to descriptors it inherited from us, so the
    // fix is at that level: for as long as the browser is up, this program's
    // own descriptors 1 and 2 point at /dev/null, which is what anything it
    // starts will inherit. Nothing of ours is written during that window - the
    // fetch is a wait, and every notice about it comes afterwards.
    //
    // Windows is left alone. The same thing there means SetStdHandle rather
    // than dup2, and the noise has not been seen from that host, so this stays
    // to what has been reproduced and can be tested.
    internal sealed class Quiet : IDisposable
    {
        private const int StandardOutput = 1;
        private const int StandardError = 2;

        // O_WRONLY. The same value on Linux and on macOS.
        private const int WriteOnly = 1;

        [DllImport("libc", SetLastError = true)]
        private static extern int dup(int descriptor);

        [DllImport("libc", SetLastError = true)]
        private static extern int dup2(int from, int to);

        [DllImport("libc", SetLastError = true)]
        private static extern int open(string path, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int descriptor);

        private int savedOutput = -1;
        private int savedError = -1;

        public Quiet()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Anything already written but still held in a buffer belongs on
            // the screen, not in /dev/null.
            Flush();

            int nowhere = open("/dev/null", WriteOnly);
            if (nowhere < 0)
            {
                return;
            }

            // Both descriptors are kept so they can be put back. If either
            // cannot be, the browser is left noisy rather than the program
            // left without a console.
            savedOutput = dup(StandardOutput);
            savedError = dup(StandardError);
            if (savedOutput < 0 || savedError < 0)
            {
                Restore();
                close(nowhere);
                return;
            }

            dup2(nowhere, StandardOutput);
            dup2(nowhere, StandardError);

            // Descriptors 1 and 2 now refer to it, so this handle has done its
            // job and would otherwise be leaked once per page read.
            close(nowhere);
        }

        public void Dispose()
        {
            Restore();
        }

        private void Restore()
        {
            Flush();
            if (savedOutput >= 0)
            {
                dup2(savedOutput, StandardOutput);
                close(savedOutput);
                savedOutput = -1;
            }
            if (savedError >= 0)
            {
                dup2(savedError, StandardError);
                close(savedError);
                savedError = -1;
            }
        }

        // Flushing through the writers rather than the streams, since that is
        // where anything written by Console is still sitting.
        private static void Flush()
        {
            try
            {
                Console.Out.Flush();
                Console.Error.Flush();
            }
            catch (Exception)
            {
                // A console that will not flush is not a reason to fail a page
                // read, and there is nothing to be done about it here.
            }
        }
    }
}
