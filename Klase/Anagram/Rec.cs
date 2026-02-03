using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klase.Anagram
{
    public class Rec
    {
        public string Recc { get; }

        private bool[] pogodili; // [0] igrac 1, [1] igrac 2

        public bool PogodjenaPrviPut => pogodili[0];

        public Rec(string rec)
        {
            Recc = rec;
            pogodili = new bool[2];
        }

        public PovratneVrednostiAnagrama Pogodi(int igracId)
        {
            int index = igracId - 1;
            int drugi = 1 - index;

            if (index < 0 || index >= pogodili.Length)
                return PovratneVrednostiAnagrama.NeispravnaRec;

            if (pogodili[index])
                return PovratneVrednostiAnagrama.VecPogodjeno;

            if (pogodili[drugi])
                return PovratneVrednostiAnagrama.DrugiIgracPogodio;

            pogodili[index] = true;
            return PovratneVrednostiAnagrama.IspravnaRec;
        }
    }
}
