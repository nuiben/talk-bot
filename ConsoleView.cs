using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace talk
{
    class ConsoleView
    {
        int phraseID = 0;

        public int ShowMenu()
        {
            Console.WriteLine("________Menu_________");
            Console.WriteLine("1) Add a phrase");
            Console.WriteLine("2) Remove a phrase");
            Console.WriteLine("3) Make Talk Bot Talk");
            Console.WriteLine("4) Run Selenium Test");
            Console.WriteLine("5) Exit");
            Console.WriteLine("______________________");
            Console.WriteLine("Make a selection (1-5)");
            return Convert.ToInt32(Console.ReadLine());
        }

        public Phrase NewPhrase()
        {
            Console.WriteLine("What phrase would you like to add?");
            string phraseName = Console.ReadLine();
            phraseID = phraseID + 1;
            Phrase toBeAdded = new Phrase(phraseID, phraseName);
            return toBeAdded;
        }

        public int DeletePhrase()
        {
            Console.WriteLine("Select Phrase ID of the phrase to be removed");
            return Convert.ToInt32(Console.ReadLine());
        }


        public int PlayPhrase()
        {
            Console.WriteLine("What is the ID of the phrase to play?");
            return Convert.ToInt32(Console.ReadLine());
        }

        public void DisplayPhrases(Phrase[] phrases)
        {
            Console.WriteLine("______Phrase List________");
            foreach (Phrase p in phrases)
            {
                Console.WriteLine(p.GetId() + ") " + p.GetPhrase());
            }
            Console.WriteLine("_______________________");
        }



    }
}
