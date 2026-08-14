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

        public void RemovePhrase(int phraseID)
        {
            phrases.Remove(phrases.Find(words => words.GetId() == phraseID));
        }

        public Phrase[] ListPhrases()
        {
            return phrases.ToArray();
        }

        // TODO: Check input validation for phrase to be played.
        public void PlayPhrase(int phraseID)
        {
            Phrase toBePlayed = phrases.Find(x => x.GetId() == phraseID);
            
            toBePlayed.Play();
        }
    }
}
