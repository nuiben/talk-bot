using System;

namespace talk.Tests
{
    // Checks that the whole story comes back, not just the part of it GitHub
    // has drawn by the time the driver looks. Beats are sampled from the top,
    // the middle and the end, so a page that renders halfway still fails.
    internal class PengyStoryTest : ITest
    {
        private static readonly string[] Beats =
        {
            PengyStory.Title,
            "Pengy is a penguin who lives in the freezer aisle",
            "Pengy had not rehearsed the part where the floor was freshly waxed.",
            "a cardboard cutout of a man recommending yogurt",
            "Pengy looked him straight in the eye and burped.",
            "And that is why, to this day, Pengy insists the floor started it."
        };

        public string Name
        {
            get { return "pengy story"; }
        }

        public bool Run()
        {
            string story = TestSuite.Flatten(PengyStory.Fetch());

            foreach (string beat in Beats)
            {
                if (!story.Contains(TestSuite.Flatten(beat)))
                {
                    TestSuite.Report("FAIL", "pengy.md came back without \"" + beat + "\"",
                        ConsoleColor.Red);
                    return false;
                }
            }

            TestSuite.Report("PASS",
                "pengy.md returned the full story (" + story.Length + " characters)",
                ConsoleColor.Green);
            return true;
        }
    }
}
