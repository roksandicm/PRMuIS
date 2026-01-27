using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// komentari se pisu iskljucivo kako bi prilikom odbrane negde zaboli da mozemo da se podsetimo jer 
// imamo dosta kolokvijuma i jos 1 odbranu projekta...
namespace Klase.Igrac
{
    internal class UlaganjeKviska
    {
        Igrac Igrac1, Igrac2;

        public UlaganjeKviska(Igrac igrac1, Igrac igrac2)
        {
            this.Igrac1 = igrac1;
            this.Igrac2 = igrac2;
        }


        public void UloziKviska(List <Socket> igraci , int indexIgre)
        {
            Console.Clear();
            Console.WriteLine("Ulozite kviska!\n");
            byte[] buffer = new byte[1024];
            string poruka = string.Empty;
            int brojBajta = 0;
            string Odgovor = string.Empty;

            string UlozenKvisko = "1. Da li zelite da ulozite kviska? (hocu/necu)";
            string OdbijenKvisko = "Vec ste ulozili kvisko u ovoj igri";

            foreach(Socket u in igraci)
            {
                if (u == igraci[0]) // provera za igraca 1
                {
                    if(Igrac1.kvisko==false) // da li je vec ulozen kvisko ili ne
                    {
                        u.Send(Encoding.UTF8.GetBytes(UlozenKvisko));
                    }
                    else
                        u.Send(Encoding.UTF8.GetBytes(OdbijenKvisko));
                }
                if (u == igraci[1]) // provera za igraca 2
                {
                    if (Igrac2.kvisko == false) // da li je vec ulozen kvisko ili ne
                    {
                        u.Send(Encoding.UTF8.GetBytes(UlozenKvisko));
                    }
                    else
                        u.Send(Encoding.UTF8.GetBytes(OdbijenKvisko));
                }
            }

            HashSet<Socket> odgovorili = new HashSet<Socket>(); // pamti ko je dao odgovor

            while (odgovorili.Count < igraci.Count) // petlja radi dok god oba igraca nisu dala neki od odgovora hocu/necu
            {
                foreach (Socket s in igraci) 
                {
            
                    if (odgovorili.Contains(s)) // ako je vec odgovorio ide se dalje i dodaje se odgovor
                        continue;
                     

                    if(s.Available>0)
                    {
                        brojBajta = s.Receive(buffer);
                        Odgovor = Encoding.UTF8.GetString(buffer, 0, brojBajta).Trim().ToLower();


                        odgovorili.Add(s);

                        if (Odgovor == "hocu")
                        {
                            if (s == igraci[0] && !Igrac1.kvisko)
                                Igrac1.UloziKviska(indexIgre); // poziva se metoda da li sme

                            if (s == igraci[1] && !Igrac2.kvisko)
                                Igrac2.UloziKviska(indexIgre); // isto se poziva metoda da li sme samo za igraca 2
                        }
                        else if (Odgovor == "necu")
                        {
                            Console.WriteLine("Nije uneo kvisko u toku ove igre!\n");
                            continue;
                        }
                    }

             
                }
            }
        }


    }
}
