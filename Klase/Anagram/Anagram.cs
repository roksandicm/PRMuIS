using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Klase.Anagram
{
    public class Anagram
    {
        public string REC { get; private set; }              // glavna rec
        public List<string> ponudjeneReci { get; private set; } = new();
        private HashSet<string> pogodjene = new();

        public Anagram(string putanja)
        {
            var linije = File.ReadAllLines(putanja);
            if (linije.Length == 0)
                throw new Exception("Prazan fajl za anagram!");

            REC = linije[0].Trim().ToUpper();               // glavna rec
            for (int i = 1; i < linije.Length; i++)
            {
                string rec = linije[i].Trim().ToUpper();
                if (!string.IsNullOrEmpty(rec))
                    ponudjeneReci.Add(rec);
            }
        }

        public int GetPogodjene() => pogodjene.Count;

        public PovratneVrednostiAnagrama PostojiRec(string unos, int id)
        {
            string rec = unos.Trim().ToUpper();

            if (!IsValidLetters(rec))              // slova koja nisu u REC
                return PovratneVrednostiAnagrama.NePostojiSlovo;

            if (pogodjene.Contains(rec))
                return PovratneVrednostiAnagrama.PreviseSlova;   // vec pogodjena

            if (ponudjeneReci.Contains(rec))
            {
                pogodjene.Add(rec);
                return PovratneVrednostiAnagrama.IspravnaRec;
            }

            return PovratneVrednostiAnagrama.NeispravnaRec;
        }

        private bool IsValidLetters(string rec)
        {
            var recUpper = rec.ToUpper();
            var recCount = new Dictionary<char, int>();
            foreach (char c in REC)
            {
                if (recCount.ContainsKey(c)) recCount[c]++;
                else recCount[c] = 1;
            }

            foreach (char c in recUpper)
            {
                if (!recCount.ContainsKey(c) || recCount[c] == 0)
                    return false;
                recCount[c]--;
            }

            return true;
        }
    }

}
