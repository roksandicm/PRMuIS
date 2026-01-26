using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klase.Igrac
{
    [Serializable]
    public class Igrac
    {
        public int id { get;}
        public string nickname { get;}

        public int brojIgre { get; private set; } // broj igara
        public int brojPoenaTrenutno { get;  set; } // trenutno stanje unutar igre

        private List<bool> penalties = new List<bool>();

        private int [] poeniUIgri; // koliko je dobijeno poena u igri

        private string[] igre;

        public bool kvisko { get; set; }
        public int IndexKvisko { get; private set; } = -1;
        public int PoeniKvisko { get; private set; } = 0;


        public void addPenalty()
        {
            penalties.Add(true);
        }

        public int getPenalties() { return penalties.Count; }

        public Igrac(string nickname)
        {
            id = new Random().Next(0,1000);
            this.nickname = nickname;
            this.brojIgre = 3;
            this.brojPoenaTrenutno = 0;
            this.igre = new string[] { "an", "po", "as" };
            poeniUIgri = new int[brojIgre];
            kvisko = false;
        }

        public Igrac(string[] prijava)
        {
            id = new Random().Next(1, 256);
            nickname = prijava[0];
            brojPoenaTrenutno = 0;
            brojIgre = prijava.Length - 1;
            igre = prijava.Skip(1).Select(x => x.Trim()).ToArray();
            poeniUIgri = new int[brojIgre];
            kvisko = false;
        }

        public void SacuvajPoene(int indexIgre)
        {
            int poeni = brojPoenaTrenutno * (indexIgre == IndexKvisko ? 2 : 1);

            if (indexIgre == IndexKvisko)
                PoeniKvisko = poeni;

            poeniUIgri[indexIgre] = poeni;
            brojPoenaTrenutno = 0;
            penalties.Clear();
        }

        public bool UloziKviska(int indexIgre)
        {
            if (kvisko) 
            return false;

            kvisko = true;
            IndexKvisko = indexIgre;
            return true;
        }

        public int GetUkupnoPoena()
        {
            return poeniUIgri.Sum();
        }

        public string GetIgra(int index)
        {
            return igre[index];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Igrač: {nickname} (ID: {id})");
            sb.AppendLine("Poeni po igrama:");

            for (int i = 0; i < brojIgre; i++)
            {
                string nazivIgre;

                switch (igre[i])
                {
                    case "an":
                        nazivIgre = "Anagram";
                        break;
                    case "po":
                        nazivIgre = "Pitanja i odgovori";
                        break;
                    case "as":
                        nazivIgre = "Asocijacije";
                        break;
                    default:
                        nazivIgre = igre[i];
                        break;
                }

                sb.AppendLine($"- {nazivIgre}: {poeniUIgri[i]} poena");
            }

            sb.AppendLine($"Ukupno poena: {GetUkupnoPoena()}");

            if (IndexKvisko != -1)
                sb.AppendLine($"Kvisko uložen u igri: {igre[IndexKvisko]} (poeni: {PoeniKvisko})");

            return sb.ToString();
        }


    }
}
