using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace talk
{
    class Phrase
    {
        private int ID;
        private string phrase;
        private SpeechSynthesizer tina;

        public Phrase(int newID, string newPhrase)
        {
            SetID(newID);
            SetPhrase(newPhrase);

            tina = new SpeechSynthesizer();
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
            SpeechSynthesizer tina = new SpeechSynthesizer();
            tina.SelectVoice("Microsoft Zira Desktop");
            tina.Speak(phrase);
        }
        
    }
}
