using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace talk
{
    class PhraseLibrary
    {
        private List<Phrase> phrases;

        // The next ID to hand out. It is kept here rather than counted from the
        // list, so that removing the last phrase does not give the next one the
        // ID that was just freed: an ID names a phrase for as long as the file
        // it was saved in is around.
        private int nextID;

        // User places phrases for the Speech Synthesizer to read.
        public PhraseLibrary()
            : this(new Phrase[0])
        {
        }

        // Phrases read back off disk start the library rather than being added
        // to it, so they keep the IDs they were saved with and the next phrase
        // typed carries on from the highest of them.
        public PhraseLibrary(Phrase[] saved)
        {
            phrases = new List<Phrase>();
            nextID = 0;
            if (saved != null)
            {
                foreach (Phrase p in saved)
                {
                    AddPhrase(p);
                }
            }
        }

        // A null is ignored rather than stored, because everything that walks
        // the list asks each phrase for its ID and its text.
        public void AddPhrase(Phrase p)
        {
            if (p == null)
            {
                return;
            }
            phrases.Add(p);
            if (p.GetId() > nextID)
            {
                nextID = p.GetId();
            }
        }

        // The ID the next phrase should be made with. Handing it out counts it
        // as used, so two phrases added in a row cannot share one.
        public int TakeNextId()
        {
            nextID = nextID + 1;
            return nextID;
        }

        // False when there is no phrase with that ID, so the view can say so
        // instead of the caller assuming it worked.
        public bool RemovePhrase(int phraseID)
        {
            Phrase toBeRemoved = phrases.Find(words => words.GetId() == phraseID);
            if (toBeRemoved == null)
            {
                return false;
            }
            return phrases.Remove(toBeRemoved);
        }

        public Phrase[] ListPhrases()
        {
            return phrases.ToArray();
        }

        // An ID that is not in the list used to end the program, which is easy
        // to hit from the menu, so a miss is reported instead.
        public bool PlayPhrase(int phraseID)
        {
            Phrase toBePlayed = phrases.Find(x => x.GetId() == phraseID);
            if (toBePlayed == null)
            {
                return false;
            }
            toBePlayed.Play();
            return true;
        }
    }
}
