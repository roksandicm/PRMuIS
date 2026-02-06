using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Klase.Pitanja_i_odgovori
{
    public class PitanjaOdgovori
    {
        public string TekucePitanje { get; private set; }
        public bool TacanOdgovor { get; private set; }

        private static Random rng = new Random();

        public Dictionary<string, bool> SvaPitanja { get; private set; } = new Dictionary<string, bool>();

        private List<string> redosledPitanja = new List<string>();
        private int indeks = 0;


        public PitanjaOdgovori(string putanjaDoTxt)
        {
            UcitajPitanjaIzTxt(putanjaDoTxt);

            redosledPitanja = new List<string>(SvaPitanja.Keys);

            IzmesajPitanja();   

            PostaviSledecePitanje();
        }

        private void IzmesajPitanja()
        {
            for (int i = redosledPitanja.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);

                string temp = redosledPitanja[i];
                redosledPitanja[i] = redosledPitanja[j];
                redosledPitanja[j] = temp;
            }
        }

        private void UcitajPitanjaIzTxt(string putanja)
        {
            foreach (string linija in File.ReadAllLines(putanja))
            {
                if (string.IsNullOrWhiteSpace(linija))
                    continue;

                string[] delovi = linija.Split('|');
                if (delovi.Length != 2) continue;

                string pitanje = delovi[0].Trim();
                string odgovor = delovi[1].Trim().ToUpper();

                bool tacno = odgovor == "DA";
                SvaPitanja.Add(pitanje, tacno);
            }
        }

        public bool PostaviSledecePitanje()
        {
            if (indeks >= redosledPitanja.Count)
                return false;

            TekucePitanje = redosledPitanja[indeks];
            TacanOdgovor = SvaPitanja[TekucePitanje];
            indeks++;

            return true;
        }

        public bool ProveriOdgovor(string odgovor)
        {
            odgovor = odgovor.Trim().ToUpper();
            if (odgovor != "DA" && odgovor != "NE")
                throw new ArgumentException("Odgovor mora biti DA ili NE");

            bool igracevOdgovor = odgovor == "DA";
            return igracevOdgovor == TacanOdgovor;
        }
    }
}
