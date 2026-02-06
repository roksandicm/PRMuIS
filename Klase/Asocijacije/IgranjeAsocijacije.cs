using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Klase.Asocijacije
{
    public class IgraAsocijacije
    {
        private KlaseZaAsocijacije.Asocijacija asocijacija;
        private int poeni = 0;
        private bool pogodjenoKonacno = false;

        public void UcitajAsocijaciju(string putanjaDoFajla)
        {
            string[] linije = File.ReadAllLines(putanjaDoFajla);
            List<KlaseZaAsocijacije.Kolona> kolone = new List<KlaseZaAsocijacije.Kolona>();

            int brojLinija = linije.Length;
            int i = 0;
            int kolonaIndex = 0;

            while (i + 4 < brojLinija) 
            {
                string nazivKolone = ((char)('A' + kolonaIndex)).ToString();

                string p1 = linije[i].Trim();
                string p2 = linije[i + 1].Trim();
                string p3 = linije[i + 2].Trim();
                string p4 = linije[i + 3].Trim();

                string resenjeKolone = linije[i + 4].Trim();

                kolone.Add(new KlaseZaAsocijacije.Kolona(nazivKolone, new List<string> { p1, p2, p3, p4 }, resenjeKolone));

                i += 5;
                kolonaIndex++;
            }

            string konacnoResenje = linije.Last().Trim();

            asocijacija = new KlaseZaAsocijacije.Asocijacija(kolone, konacnoResenje);
        }

       
        public void PrikaziAsocijaciju()
        {
            foreach (var kolona in asocijacija.Kolone)
            {
                Console.WriteLine($"Kolona {kolona.Naziv}:");
                for (int i = 0; i < kolona.Polja.Count; i++)
                {
                    Console.WriteLine($"{kolona.Naziv}{i + 1}: {kolona.Polja[i]}");
                }
                Console.WriteLine($"Rešenje kolone {kolona.Naziv}: {(kolona.Pogodjeno ? kolona.KonacnoResenje : "***")}");
                Console.WriteLine(new string('-', 30));
            }

            Console.WriteLine($"Konačno rešenje: {(pogodjenoKonacno ? asocijacija.KonacnoResenje : "***")}");
        }

        
        public bool OtvoriPolje(string unos)
        {
            if (unos.Length != 2) return false;
            char kolonaChar = char.ToUpper(unos[0]);
            if (!int.TryParse(unos[1].ToString(), out int index)) return false;
            index -= 1;

            var kolona = asocijacija.Kolone.FirstOrDefault(k => k.Naziv[0] == kolonaChar);
            if (kolona == null || index < 0 || index >= kolona.Polja.Count) return false;

            kolona.Polja[index].Otvori();
            return true;
        }

       
        public bool PogodiKolonu(string unos)
        {
            if (!unos.Contains(':')) return false;

            char kolonaChar = char.ToUpper(unos[0]);
            string resenje = unos.Substring(2).Trim();

            var kolona = asocijacija.Kolone.FirstOrDefault(k => k.Naziv[0] == kolonaChar);
            if (kolona == null) return false;

            if (kolona.KonacnoResenje.Equals(resenje, StringComparison.OrdinalIgnoreCase))
            {
                kolona.Pogodjeno = true;

              
                int brojNeotvorenih = kolona.Polja.Count(p => !p.Otvoreno);
                poeni += brojNeotvorenih * 2 + 2;

                
                foreach (var polje in kolona.Polja)
                    polje.Otvori();

                return true;
            }

            return false;
        }

        public bool PogodiKonacno(string unos, out bool kraj)
        {
            kraj = false;
            if (!unos.StartsWith("K:", StringComparison.OrdinalIgnoreCase)) return false;

            string resenje = unos.Substring(2).Trim();
            if (asocijacija.KonacnoResenje.Equals(resenje, StringComparison.OrdinalIgnoreCase))
            {
                pogodjenoKonacno = true;
                poeni += 10; 

                foreach (var kolona in asocijacija.Kolone)
                {
                    if (!kolona.Pogodjeno)
                    {
                        kolona.Pogodjeno = true;
                        int brojNeotvorenih = kolona.Polja.Count(p => !p.Otvoreno);
                        poeni += brojNeotvorenih * 2 + 2;

                        foreach (var polje in kolona.Polja)
                            polje.Otvori();
                    }
                    else
                    {
                        foreach (var polje in kolona.Polja)
                            polje.Otvori();
                    }
                }

                kraj = true; 
                return true;
            }

            return false;
        }


        public string PrikaziStanje()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var kolona in asocijacija.Kolone)
            {
                sb.AppendLine($"Kolona {kolona.Naziv}:");
                for (int i = 0; i < kolona.Polja.Count; i++)
                {
                    sb.AppendLine($"{kolona.Naziv}{i + 1}: {kolona.Polja[i]}");
                }
                sb.AppendLine($"Rešenje kolone {kolona.Naziv}: {(kolona.Pogodjeno ? kolona.KonacnoResenje : "***")}");
                sb.AppendLine(new string('-', 20));
            }

            sb.AppendLine($"Konačno rešenje: {(pogodjenoKonacno ? asocijacija.KonacnoResenje : "***")}");
            return sb.ToString();
        }

        public int GetPoeni()
        {
            return poeni;
        }
    }
}