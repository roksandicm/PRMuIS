using Klase;
using Klase.Igrac;
using Klase.Anagram;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Projekat
{
    public class Server
    {
        private static Igrac igrac;
        private static List<Igrac> igraci = new List<Igrac>();
        private static List<EndPoint> endPointIgraca = new List<EndPoint>();
        private static string baseDir = AppContext.BaseDirectory;

        private static Dictionary<string, Igrac> tokenMap = new Dictionary<string, Igrac>();

        static void Main(string[] args)
        {
            udpKonekcija();
        }

        static void udpKonekcija()
        {
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, 51001);
            udpSocket.Bind(udpEP);
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("UDP server je pokrenut..\n");

            while (true)
            {
                byte[] bafer = new byte[1024];
                try
                {
                    int bytesRec = udpSocket.ReceiveFrom(bafer, ref sender);
                    string poruka = Encoding.UTF8.GetString(bafer, 0, bytesRec);

                    Console.WriteLine($"{sender}: {poruka}");

                    string[] deloviPoruke = poruka.Split(',');

                    if (deloviPoruke.Length == 1)
                    {
                        Igrac novi = new Igrac(deloviPoruke[0]);
                        igraci.Add(novi);
                        endPointIgraca.Add(sender);

                        string token = Guid.NewGuid().ToString("N");
                        tokenMap[token] = novi;
                        string tokenMsg = "token," + token;
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(tokenMsg), sender);

                        if (igraci.Count == 2)
                        {
                            poruka = "igra";
                            bafer = Encoding.UTF8.GetBytes(poruka);
                            foreach (EndPoint ep in endPointIgraca)
                                udpSocket.SendTo(bafer, ep);

                            Console.WriteLine("\n-----------------------------------------------------------------");
                            Console.WriteLine("\t\t\tPOKRETANJE KVIZA");
                            Console.WriteLine("-----------------------------------------------------------------\n");
                            Thread.Sleep(1500);
                            Console.Clear();

                            igraDvaIgraca(); 
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Potreban je jos jedan igrac...");
                        }
                    }
                    else if (deloviPoruke.Length > 1)
                    {
                        igrac = new Igrac(deloviPoruke);
                        poruka = "trening";
                        bafer = Encoding.UTF8.GetBytes(poruka);
                        udpSocket.SendTo(bafer, sender);

                        Console.WriteLine("\n-----------------------------------------------------------------");
                        Console.WriteLine("\t\t\tPOKRETANJE TRENINGA");
                        Console.WriteLine("-----------------------------------------------------------------\n");
                        Thread.Sleep(1500);
                        Console.Clear();
                        treningJedanIgrac();
                        break;
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void treningJedanIgrac()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 51002);
            tcpSocket.Bind(ep);
            tcpSocket.Listen(1);

            Console.WriteLine("TCP server je poceo i čeka klijenta...");
            Socket acceptSocket = tcpSocket.Accept();
            Console.WriteLine($"Povezao se klijent: {(acceptSocket.RemoteEndPoint as IPEndPoint)?.Address}");

            string msg = $"Dobrodošli u trening igru kviza Kviskoteka, današnji takmičar je {igrac.nickname}";
            byte[] bufferSent = Encoding.UTF8.GetBytes(msg);
            acceptSocket.Send(bufferSent);

            byte[] bufferRec = new byte[1024];
            string clientMsg = "";

            while (clientMsg.ToUpper() != "START")
            {
                int bytesRec = acceptSocket.Receive(bufferRec);
                clientMsg = Encoding.UTF8.GetString(bufferRec, 0, bytesRec);
                if (clientMsg.ToUpper() != "START")
                {
                    msg = "Unesite 'START' za početak igre:";
                    bufferSent = Encoding.UTF8.GetBytes(msg);
                    acceptSocket.Send(bufferSent);
                }
            }

            msg = igrac.brojIgre.ToString();
            bufferSent = Encoding.UTF8.GetBytes(msg);
            acceptSocket.Send(bufferSent);

            for (int i = 0; i < igrac.brojIgre; i++)
            {
                string nazIgre = igrac.GetIgra(i);
                if (nazIgre == "an")
                {
                    try
                    {
                        msg = "ANAGRAM";
                        bufferSent = Encoding.UTF8.GetBytes(msg);
                        acceptSocket.Send(bufferSent);

                        string putanja = Path.Combine(baseDir, "FajloviZaIgre", "Anagrami", "anagram2.txt");
                        Anagram anagram = new Anagram(putanja);

                        Thread.Sleep(10);
                        msg = anagram.REC;
                        bufferSent = Encoding.UTF8.GetBytes(msg);
                        acceptSocket.Send(bufferSent);

                        Console.WriteLine("Pokrenuta igra ANAGRAM");
                        Console.WriteLine("Glavna reč: " + anagram.REC);

                        bool igraTraje = true;
                        while (igraTraje)
                        {
                            if (anagram.GetPogodjene() >= anagram.ponudjeneReci.Count)
                            {
                                msg = $"KRAJ ANAGRAMA! Ukupno poeni: {igrac.brojPoenaTrenutno}";
                                bufferSent = Encoding.UTF8.GetBytes(msg);
                                acceptSocket.Send(bufferSent);
                                Console.WriteLine("Igra je završena!");
                                break;
                            }

                            int bytesRecOdgovor = acceptSocket.Receive(bufferRec);
                            string odgovor = Encoding.UTF8.GetString(bufferRec, 0, bytesRecOdgovor).Trim();
                            Console.WriteLine($"Odgovor klijenta: {odgovor}");

                            if (odgovor.ToUpper() == "ODUSTAJEM" || odgovor.ToUpper() == "KRAJ")
                            {
                                msg = $"Odustali ste ili je kraj igre! Poeni: {igrac.brojPoenaTrenutno}";
                                bufferSent = Encoding.UTF8.GetBytes(msg);
                                acceptSocket.Send(bufferSent);
                                break;
                            }

                            var rezultat = anagram.PostojiRec(odgovor, igrac.id);
                            switch (rezultat)
                            {
                                case PovratneVrednostiAnagrama.IspravnaRec:
                                    int poeni = odgovor.Replace(" ", "").Length;
                                    igrac.brojPoenaTrenutno += poeni;
                                    msg = $"TACNO! +{poeni} poena. Pogodjeno {anagram.GetPogodjene()}/{anagram.ponudjeneReci.Count}";
                                    bufferSent = Encoding.UTF8.GetBytes(msg);
                                    acceptSocket.Send(bufferSent);
                                    break;

                                case PovratneVrednostiAnagrama.NePostojiSlovo:
                                    msg = "NETACNO! Uneli ste slovo koje ne postoji u glavnoj reci.";
                                    bufferSent = Encoding.UTF8.GetBytes(msg);
                                    acceptSocket.Send(bufferSent);
                                    break;

                                case PovratneVrednostiAnagrama.PreviseSlova:
                                    msg = "NETACNO! Previše puta ste upotrebili neko slovo.";
                                    bufferSent = Encoding.UTF8.GetBytes(msg);
                                    acceptSocket.Send(bufferSent);
                                    break;

                                case PovratneVrednostiAnagrama.NeispravnaRec:
                                    msg = "NETACNO! Ta reč nije predložena za ovaj anagram.";
                                    bufferSent = Encoding.UTF8.GetBytes(msg);
                                    acceptSocket.Send(bufferSent);
                                    break;
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    igrac.SacuvajPoene(i);
                }
            }

            Console.WriteLine("\n-----------------------------------------------------------------\n");
            Console.WriteLine("\t\t\tPODACI O IGRACU");
            Console.WriteLine(igrac);
            Console.WriteLine("-----------------------------------------------------------------\n");

            acceptSocket.Close();
            tcpSocket.Close();
        }

        static void igraDvaIgraca()
        {
            int maxKlijenata = 2;

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 51002));
            serverSocket.Listen(10);
            serverSocket.Blocking = false;

            Dictionary<Socket, Igrac> mapa = new Dictionary<Socket, Igrac>();
            Dictionary<Socket, bool> ready = new Dictionary<Socket, bool>();
            byte[] buffer = new byte[256];

            while (true)
            {
                if (mapa.Count < maxKlijenata && serverSocket.Poll(1000 * 1000, SelectMode.SelectRead))
                {
                    Socket client = serverSocket.Accept();
                    client.Blocking = true; 

                    int n = 0;
                    try { n = client.Receive(buffer); } catch { n = 0; }
                    if (n == 0) { client.Close(); continue; }

                    string first = Encoding.UTF8.GetString(buffer, 0, n).Trim();
                    var parts = first.Split(',');

                    if (parts.Length != 2 || !parts[0].Equals("token", StringComparison.OrdinalIgnoreCase))
                    {
                        client.Send(Encoding.UTF8.GetBytes("los_token"));
                        client.Close();
                        continue;
                    }

                    string token = parts[1].Trim();

                    if (!tokenMap.TryGetValue(token, out Igrac ig))
                    {
                        client.Send(Encoding.UTF8.GetBytes("nepoznat_token"));
                        client.Close();
                        continue;
                    }

                    if (mapa.Values.Contains(ig))
                    {
                        client.Send(Encoding.UTF8.GetBytes("token_vec_koriscen"));
                        client.Close();
                        continue;
                    }

                    client.Blocking = false;
                    mapa[client] = ig;
                    ready[client] = false;

                    Console.WriteLine($"SERVER: Povezan igrac {ig.nickname} {client.RemoteEndPoint}");
                    client.Send(Encoding.UTF8.GetBytes("ok_token"));
                }

                foreach (var s in mapa.Keys.ToArray())
                {
                    if (!s.Poll(1000 * 1000, SelectMode.SelectRead))
                        continue;

                    int br = 0;
                    try { br = s.Receive(buffer); } catch { br = 0; }

                    if (br == 0)
                    {
                        Console.WriteLine($"SERVER: Igrac {mapa[s].nickname} se diskonektovao");
                        try { s.Close(); } catch { }
                        ready.Remove(s);
                        mapa.Remove(s);
                        continue;
                    }

                    string poruka = Encoding.UTF8.GetString(buffer, 0, br).Trim();

                    if (poruka.Equals("start", StringComparison.OrdinalIgnoreCase))
                    {
                        ready[s] = true;
                        Console.WriteLine($"SERVER: {mapa[s].nickname} je spreman");
                        s.Send(Encoding.UTF8.GetBytes("ok_start"));
                    }
                }

                if (mapa.Count == maxKlijenata && ready.Count == maxKlijenata && ready.Values.All(v => v))
                {
                    Console.WriteLine("SERVER: Oba igraca spremna.");

                    foreach (Socket s in mapa.Keys)
                        s.Send(Encoding.UTF8.GetBytes("zapocinjem"));

                    Thread.Sleep(1000);
                    Console.Clear();
                    break;
                }

                Thread.Sleep(50);
            }
        }
    }
}
