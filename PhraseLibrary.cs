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

        // User places phrases for the Speech Synthesizer to read.
        public PhraseLibrary()
        {
            phrases = new List<Phrase>();
          
        }

        public void AddPhrase(Phrase p)
        {
            phrases.Add(p);
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
