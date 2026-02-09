using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Klase.Igrac
{
    public class UlaganjeKviska
    {
        Igrac Igrac1, Igrac2;

        public UlaganjeKviska(Igrac igrac1, Igrac igrac2)
        {
            this.Igrac1 = igrac1;
            this.Igrac2 = igrac2;
        }


        public void UloziKviska(List<Socket> igraci, int indexIgre)
        {
            Console.Clear();
            Console.WriteLine("Ulaganje kviska!\n");
            byte[] buffer = new byte[1024];
            string poruka = string.Empty;
            int brojBajta = 0;
            string Odgovor = string.Empty;

            string UlozenKvisko = "KVISKO|Da li zelite da ulozite kviska? (hocu/necu)";
            string OdbijenKvisko = "KVISKO|Vec ste ulozili kviska.";

            foreach (Socket u in igraci)
            {
                if (u == igraci[0])
                {
                    if (Igrac1.kvisko == false)
                    {
                        u.Send(Encoding.UTF8.GetBytes(UlozenKvisko));
                    }
                    else
                        u.Send(Encoding.UTF8.GetBytes(OdbijenKvisko));
                }
                if (u == igraci[1])
                {
                    if (Igrac2.kvisko == false)
                    {
                        u.Send(Encoding.UTF8.GetBytes(UlozenKvisko));
                    }
                    else
                        u.Send(Encoding.UTF8.GetBytes(OdbijenKvisko));
                }
            }

            HashSet<Socket> odgovorili = new HashSet<Socket>();

            if (igraci.Count > 0 && Igrac1.kvisko)
                odgovorili.Add(igraci[0]);
            if (igraci.Count > 1 && Igrac2.kvisko)
                odgovorili.Add(igraci[1]);

            while (odgovorili.Count < igraci.Count)
            {
                foreach (Socket s in igraci)
                {

                    if (odgovorili.Contains(s))
                        continue;


                    if (s.Available > 0)
                    {
                        brojBajta = s.Receive(buffer);
                        Odgovor = Encoding.UTF8.GetString(buffer, 0, brojBajta).Trim().ToLower();


                        odgovorili.Add(s);

                        if (Odgovor == "hocu")
                        {
                            if (s == igraci[0] && !Igrac1.kvisko)
                                Igrac1.UloziKviska(indexIgre);

                            if (s == igraci[1] && !Igrac2.kvisko)
                                Igrac2.UloziKviska(indexIgre);
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
