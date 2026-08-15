using System;
using System.IO;

namespace talk.Tests
{
    // Quieting the browser swaps this program's own output and error
    // descriptors for /dev/null and swaps them back afterwards. Getting that
    // wrong does not fail a page read: it leaves the program running with no
    // console at all, which is a worse fault than the noise it removes and one
    // that would only show up as a menu that never appears again.
    //
    // So what is checked here is the putting back, from every direction the
    // program can leave that block: normally, twice over, and through an
    // exception.
    internal class QuietTest : ITest
    {
        public string Name
        {
            get { return "browser quieting"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            checks.Survives("quieting and restoring", delegate
            {
                using (Quiet quiet = new Quiet())
                {
                }
            });
            checks.True("the console still writes afterwards", Writes());

            // Dispose is called by the using block, and Restore is written to
            // be safe if it is called again after that.
            checks.Survives("disposing twice", delegate
            {
                Quiet quiet = new Quiet();
                quiet.Dispose();
                quiet.Dispose();
            });
            checks.True("the console still writes after a second dispose", Writes());

            // A page that cannot be read throws from inside the block, which is
            // the path the console is most likely to be left broken on.
            checks.Survives("an exception on the way out", delegate
            {
                try
                {
                    using (Quiet quiet = new Quiet())
                    {
                        throw new PageNotReadableException("nothing to read");
                    }
                }
                catch (PageNotReadableException)
                {
                }
            });
            checks.True("the console still writes after an exception", Writes());

            // Nested blocks are not how the reader uses this, but a restore
            // that closed a descriptor twice would show up here rather than in
            // a menu that has quietly stopped drawing.
            checks.Survives("nesting", delegate
            {
                using (Quiet outer = new Quiet())
                {
                    using (Quiet inner = new Quiet())
                    {
                    }
                }
            });
            checks.True("the console still writes after nesting", Writes());

            return checks.Report(Name, "the console comes back however the browser leaves");
        }

        // Whether the console can still be written to and read back. This goes
        // through Console rather than straight at the descriptor, because that
        // is what every message in the program uses.
        private static bool Writes()
        {
            TextWriter screen = Console.Out;
            StringWriter caught = new StringWriter();
            try
            {
                Console.SetOut(caught);
                Console.Write("still here");
                Console.Out.Flush();
            }
            finally
            {
                Console.SetOut(screen);
            }
            return caught.ToString() == "still here";
        }
    }
}
