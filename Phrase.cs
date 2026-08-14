using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace talk
{
    class Phrase
    {
        private int ID;
        private string phrase;

        public Phrase(int newID, string newPhrase)
        {
            SetID(newID);
            SetPhrase(newPhrase);
        }

        public int GetId()
        {
            return ID;
        }

        private void SetID(int newID)
        {
            ID = newID;
        }

        public string GetPhrase()
        {
            return phrase;
        }

        public void SetPhrase(string newPhrase)
        {
            phrase = newPhrase;
        }

        public void Play()
        {
            SpeechEngine.Current.Speak(phrase);
        }
        
    }
}
