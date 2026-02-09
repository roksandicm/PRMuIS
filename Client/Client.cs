using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    internal class Client
    {
        static string multiplayerToken = null;

        static void Main(string[] args)
        {
            udpKonekcija();
        }

        static void udpKonekcija()
        {
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint targetEP = new IPEndPoint(IPAddress.Loopback, 51001);
            EndPoint recEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] Sendbuffer;
            byte[] Recbuffer = new byte[1024];

            while (true)
            {
                Console.Write("Prijava: ");
                string msg = Console.ReadLine();
                Sendbuffer = Encoding.UTF8.GetBytes(msg);

                try
                {
                    udpSocket.SendTo(Sendbuffer, targetEP);

                    multiplayerToken = null;
                    bool gotGameOrTraining = false;

                    while (!gotGameOrTraining)
                    {
                        int bytesRec = udpSocket.ReceiveFrom(Recbuffer, ref recEP);
                        string resp = Encoding.UTF8.GetString(Recbuffer, 0, bytesRec).Trim();

                        if (resp.StartsWith("token,", StringComparison.OrdinalIgnoreCase))
                        {
                            var p = resp.Split(',');
                            if (p.Length == 2)
                            {
                                multiplayerToken = p[1].Trim();
                            }
                            continue;
                        }

                        if (resp == "trening")
                        {
                            Console.WriteLine("Zapocinjanje treninga...");
                            Thread.Sleep(1000);
                            Console.Clear();
                            samoTrening();
                            gotGameOrTraining = true;
                            break;
                        }
                        else if (resp == "igra")
                        {
                            Console.WriteLine("Zapocinjanje kviza...");
                            Thread.Sleep(1000);
                            Console.Clear();
                            kviz();
                            gotGameOrTraining = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine(resp);
                            gotGameOrTraining = true;
                            break;
                        }
                    }

                    if (gotGameOrTraining)
                        break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            udpSocket.Close();
        }

        static void samoTrening()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 51002);
            byte[] bufferRec = new byte[4096];
            byte[] bufferSent;
            tcpSocket.Connect(ep);

            try
            {

                int recByte = tcpSocket.Receive(bufferRec);
                string msg = Encoding.UTF8.GetString(bufferRec, 0, recByte);
                Console.WriteLine(msg);


                string zaStart = "";
                while (zaStart.Trim().ToUpper() != "START")
                {
                    Console.Write("Unesite 'START' za pocetak: ");
                    zaStart = Console.ReadLine();
                }
                bufferSent = Encoding.UTF8.GetBytes(zaStart.Trim());
                tcpSocket.Send(bufferSent);


                recByte = tcpSocket.Receive(bufferRec);
                int brIgara = int.Parse(Encoding.UTF8.GetString(bufferRec, 0, recByte));

                for (int i = 0; i < brIgara; i++)
                {

                    recByte = tcpSocket.Receive(bufferRec);
                    msg = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();

                    Console.WriteLine($"Igra {i + 1}: {msg}");


                    if (msg == "ANAGRAM")
                    {
                        recByte = tcpSocket.Receive(bufferRec);
                        string glavnaRec = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                        Console.WriteLine("Glavna reč: " + glavnaRec);

                        bool igraTraje = true;
                        while (igraTraje)
                        {
                            Console.Write("Unesite odgovor (ili 'ODUSTAJEM'/ 'KRAJ'): ");
                            string odgovor = Console.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(odgovor)) continue;

                            bufferSent = Encoding.UTF8.GetBytes(odgovor);
                            tcpSocket.Send(bufferSent);

                            recByte = tcpSocket.Receive(bufferRec);
                            msg = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                            Console.WriteLine(msg);

                            if (msg.ToUpper().Contains("KRAJ") || odgovor.ToUpper() == "KRAJ" || odgovor.ToUpper() == "ODUSTAJEM")
                                igraTraje = false;
                        }
                    }


                    if (msg == "PITANJA_IODGOVORI" || msg == "PIO")
                    {
                        Console.WriteLine("\n--- PITANJA I ODGOVORI ---\n");
                        bool igraTraje = true;

                        while (igraTraje)
                        {
                            recByte = tcpSocket.Receive(bufferRec);
                            string pitanje = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();

                            if (pitanje.ToUpper().Contains("KRAJ"))
                            {
                                Console.WriteLine(pitanje);
                                break;
                            }

                            Console.WriteLine(pitanje);

                            string odgovor = "";
                            while (true)
                            {
                                Console.Write("Odgovor (DA/NE): ");
                                odgovor = Console.ReadLine().Trim().ToUpper();

                                if (odgovor == "DA" || odgovor == "NE" || odgovor == "KRAJ" || odgovor == "ODUSTAJEM")
                                    break;

                                Console.WriteLine("Nevalidan unos!");
                            }

                            bufferSent = Encoding.UTF8.GetBytes(odgovor);
                            tcpSocket.Send(bufferSent);

                            recByte = tcpSocket.Receive(bufferRec);
                            string rezultat = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                            Console.WriteLine(rezultat);

                            if (rezultat.ToUpper().Contains("KRAJ"))
                                igraTraje = false;
                        }

                        Console.WriteLine("\n--- Kraj PIO igre ---\n");
                    }


                    if (msg == "ASOCIJACIJE" || msg == "AS")
                    {
                        Console.WriteLine("\n--- ASOCIJACIJE ---\n");
                        bool igraTraje = true;

                        while (igraTraje)
                        {
                            recByte = tcpSocket.Receive(bufferRec);
                            string stanje = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                            Console.WriteLine(stanje);

                            Console.Write("Unesite potez (polje npr. A1, kolonu npr. A:resenje, konačno K:resenje ili KRAJ/ODUSTAJEM): ");
                            string unos = Console.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(unos)) continue;

                            tcpSocket.Send(Encoding.UTF8.GetBytes(unos));

                            recByte = tcpSocket.Receive(bufferRec);
                            string rezultat = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                            Console.WriteLine(rezultat);

                            if (unos.StartsWith("K:", StringComparison.OrdinalIgnoreCase) ||
                                unos.Equals("KRAJ", StringComparison.OrdinalIgnoreCase) ||
                                unos.Equals("ODUSTAJEM", StringComparison.OrdinalIgnoreCase))
                            {
                                recByte = tcpSocket.Receive(bufferRec);
                                string krajPoruka = Encoding.UTF8.GetString(bufferRec, 0, recByte).Trim();
                                Console.WriteLine(krajPoruka);

                                igraTraje = false;
                                Console.WriteLine("\n--- Kraj ASOCIJACIJA ---\n");
                            }
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }

            tcpSocket.Close();
        }

        static void kviz()
        {
            Socket TCPclientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint TCPserverEP = new IPEndPoint(IPAddress.Loopback, 51002);
            TCPclientSocket.Connect(TCPserverEP);

            byte[] buffer = new byte[8192];

            // TOKEN
            if (string.IsNullOrWhiteSpace(multiplayerToken))
            {
                Console.WriteLine("Greska: nije primljen token preko UDP-a.");
                TCPclientSocket.Close();
                return;
            }

            TCPclientSocket.Send(Encoding.UTF8.GetBytes("token," + multiplayerToken));

            int br = TCPclientSocket.Receive(buffer);
            if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); TCPclientSocket.Close(); return; }

            string resp = Encoding.UTF8.GetString(buffer, 0, br).Trim();
            if (resp != "ok_token")
            {
                Console.WriteLine("Server odbio token: " + resp);
                TCPclientSocket.Close();
                return;
            }

            // START
            string poruka;
            do
            {
                Console.Write("Unesite \"START\" za pocetak kviza: ");
                poruka = Console.ReadLine();
            } while (!poruka.Equals("START", StringComparison.OrdinalIgnoreCase));

            TCPclientSocket.Send(Encoding.UTF8.GetBytes("start"));

            // cekaj zapocinjem
            while (true)
            {
                br = TCPclientSocket.Receive(buffer);
                if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); TCPclientSocket.Close(); return; }

                string msg = Encoding.UTF8.GetString(buffer, 0, br).Trim();
                if (msg.Equals("zapocinjem", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine(msg);
            }

            Console.WriteLine("\nKviz pocinje!\n");

            int game = 0;

            while (true)
            {
                br = TCPclientSocket.Receive(buffer);
                if (br == 0)
                {
                    Console.WriteLine("Server zatvorio konekciju.");
                    break;
                }

                string igra = Encoding.UTF8.GetString(buffer, 0, br).Trim();

                // Ulaganje Kviska (server salje KVISKO|...)
                if (igra.StartsWith("KVISKO|", StringComparison.OrdinalIgnoreCase))
                {
                    string tekst = igra.Substring("KVISKO|".Length);
                    Console.WriteLine("" + tekst);

                    if (!tekst.Contains("Vec ste ulozili", StringComparison.OrdinalIgnoreCase))
                    {
                        string ans;
                        do
                        {
                            Console.Write("Odgovor: ");
                            ans = Console.ReadLine().Trim().ToLower();
                        } while (ans != "hocu" && ans != "necu");

                        try { TCPclientSocket.Send(Encoding.UTF8.GetBytes(ans)); } catch { }
                    }

                    continue;
                }

                if (igra.Equals("KRAJ_KVIZA", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                game++;
                Console.WriteLine($"\n=== IGRA {game}: {igra} ===\n");

                // ANAGRAM
                if (igra.Equals("ANAGRAM", StringComparison.OrdinalIgnoreCase))
                {
                    br = TCPclientSocket.Receive(buffer);
                    if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); break; }

                    string glavnaRec = Encoding.UTF8.GetString(buffer, 0, br).Trim();
                    Console.WriteLine("Glavna rec: " + glavnaRec);
                    Console.WriteLine("Kucaj reci (ili 'KRAJ' / 'ODUSTAJEM'):\n");

                    bool jaZavrsio = false;

                    while (true)
                    {
                        if (!jaZavrsio)
                        {
                            Console.Write("> ");
                            string unos = Console.ReadLine();
                            if (string.IsNullOrWhiteSpace(unos)) continue;

                            try { TCPclientSocket.Send(Encoding.UTF8.GetBytes(unos)); }
                            catch { Console.WriteLine("Pukla veza sa serverom."); return; }

                            if (unos.Equals("kraj", StringComparison.OrdinalIgnoreCase) ||
                                unos.Equals("odustajem", StringComparison.OrdinalIgnoreCase))
                            {
                                jaZavrsio = true;
                                Console.WriteLine("(Zavrsio si. Cekas drugog igraca...)");
                            }
                        }

                        br = TCPclientSocket.Receive(buffer);
                        if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); return; }

                        string msg = Encoding.UTF8.GetString(buffer, 0, br).Trim();
                        Console.WriteLine(msg);

                        if (msg.StartsWith("KRAJ ANAGRAMA", StringComparison.OrdinalIgnoreCase))
                            break;
                    }

                    Console.WriteLine("\n--- Kraj ANAGRAMA ---\n");
                    continue;
                }

                // PITANJA I ODGOVORI (PIO)
                if (igra.Equals("PITANJA_IODGOVORI", StringComparison.OrdinalIgnoreCase) || igra.Equals("PIO", StringComparison.OrdinalIgnoreCase))
                {
                    bool jaZavrsio = false;

                    while (true)
                    {
                        int nByte = TCPclientSocket.Receive(buffer);
                        if (nByte == 0) break;

                        string serverMsg = Encoding.UTF8.GetString(buffer, 0, nByte).Trim();
                        string[] delovi = serverMsg.Split('|');
                        string komanda = delovi[0];

                        if (komanda == "PITANJE")
                        {
                            Console.WriteLine("\n" + delovi[1]);

                            if (!jaZavrsio)
                            {
                                Console.Write("Vaš odgovor (DA/NE): ");
                                string unos = Console.ReadLine().Trim().ToUpper();
                                if (string.IsNullOrWhiteSpace(unos)) unos = " ";

                                TCPclientSocket.Send(Encoding.UTF8.GetBytes(unos));

                                if (unos == "KRAJ" || unos == "ODUSTAJEM")
                                {
                                    jaZavrsio = true;
                                    Console.WriteLine("(Odustali ste. Čekate kraj partije...)");
                                }
                            }
                        }
                        else if (komanda == "REZULTAT")
                        {
                            Console.WriteLine(delovi[1]);
                        }
                        else if (komanda == "KRAJ_PIO")
                        {
                            Console.WriteLine("\n*******************************");
                            Console.WriteLine(delovi[1]);
                            Console.WriteLine("*******************************");

                            //Console.WriteLine("\nPritisnite ENTER za potvrdu i nastavak...");
                            //Console.ReadLine();

                            // SINHRONIZACIJA
                            //TCPclientSocket.Send(Encoding.UTF8.GetBytes("GOTOVO_PIO"));
                            break;
                        }
                    }
                    continue;


                }





                if (igra.Equals("ASOCIJACIJE", StringComparison.OrdinalIgnoreCase) || igra.Equals("AS", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n--- ASOCIJACIJE (MP) ---\n");

                    string pending = "";
                    string RecvLine()
                    {
                        while (true)
                        {
                            int nl = pending.IndexOf('\n');
                            if (nl >= 0)
                            {
                                string line = pending.Substring(0, nl);
                                pending = pending.Substring(nl + 1);
                                return line.TrimEnd('\r');
                            }

                            int got = TCPclientSocket.Receive(buffer);
                            if (got == 0) return null;
                            pending += Encoding.UTF8.GetString(buffer, 0, got);
                        }
                    }

                    while (true)
                    {
                        string serverMsg = RecvLine();
                        if (serverMsg == null) { Console.WriteLine("Server zatvorio konekciju."); return; }
                        serverMsg = serverMsg.Trim();

                        if (!serverMsg.Contains("|"))
                        {
                            Console.WriteLine(serverMsg);
                            continue;
                        }

                        string[] delovi = serverMsg.Split('|');
                        string komanda = delovi[0];

                        if (komanda == "STANJE")
                        {
                            string stanje = serverMsg.Substring("STANJE|".Length).Replace("\\n", "\n");
                            Console.WriteLine(stanje);
                        }
                        else if (komanda == "REZULTAT")
                        {
                            Console.WriteLine(delovi.Length > 1 ? delovi[1] : "");
                        }
                        else if (komanda == "CEKAJ")
                        {
                            Console.WriteLine(delovi.Length > 1 ? delovi[1] : "Cekaj...");
                        }
                        else if (komanda == "TVOJ_POTEZ")
                        {
                            Console.WriteLine(delovi.Length > 1 ? delovi[1] : "Tvoj potez:");

                            Console.Write("> ");
                            string unos = Console.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(unos)) unos = " ";

                            TCPclientSocket.Send(Encoding.UTF8.GetBytes(unos + "\n"));
                        }
                        else if (komanda == "KRAJ_AS")
                        {
                            Console.WriteLine("\n*******************************");
                            Console.WriteLine(delovi.Length > 1 ? delovi[1] : "Kraj Asocijacija.");
                            Console.WriteLine("*******************************\n");
                            break;
                        }
                        else
                        {
                            Console.WriteLine(serverMsg);
                        }
                    }


                    Console.WriteLine("\n--- Kraj ASOCIJACIJA (MP) ---\n");
                    Thread.Sleep(1000);
                    continue;
                }

                //Console.WriteLine("Nepoznata ili neimplementirana igra: " + igra);
            }



            Console.WriteLine("\nKviz zavrsen!\n");
            Thread.Sleep(4000);
            TCPclientSocket.Close();
        }



    }
}
