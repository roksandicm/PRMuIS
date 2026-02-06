using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klase.Asocijacije
{
    public class KlaseZaAsocijacije
    {
        public class Polje
        {
            public string Sadrzaj { get; set; }
            public bool Otvoreno { get; set; } = false;

            public Polje(string sadrzaj)
            {
                Sadrzaj = sadrzaj;
            }

            public void Otvori()
            {
                Otvoreno = true;
            }

            public override string ToString()
            {
                return Otvoreno ? Sadrzaj : "***";
            }
        }

        public class Kolona
        {
            public string Naziv { get; set; }
            public List<Polje> Polja { get; set; }
            public string KonacnoResenje { get; set; }
            public bool Pogodjeno { get; set; } = false;

            public Kolona(string naziv, List<string> polja, string konacnoResenje)
            {
                Naziv = naziv;
                Polja = polja.Select(p => new Polje(p)).ToList();
                KonacnoResenje = konacnoResenje;
            }
        }

        public class Asocijacija
        {
            public List<Kolona> Kolone { get; set; }
            public string KonacnoResenje { get; set; }

            public Asocijacija(List<Kolona> kolone, string konacnoResenje)
            {
                Kolone = kolone;
                KonacnoResenje = konacnoResenje;
            }
        }
    }
}
