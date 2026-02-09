using Klase;
using Klase.Anagram;
using Klase.Asocijacije;
using Klase.Igrac;
using Klase.Pitanja_i_odgovori;
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
                else if (nazIgre == "pio")
                {
                    try
                    {
                        msg = "PITANJA_IODGOVORI";
                        bufferSent = Encoding.UTF8.GetBytes(msg);
                        acceptSocket.Send(bufferSent);

                        string putanja = Path.Combine(baseDir, "FajloviZaIgre", "PiO", "pitanja.txt");
                        PitanjaOdgovori igra = new PitanjaOdgovori(putanja);

                        Console.WriteLine("Pokrenuta igra PITANJA I ODGOVORI");

                        int maxPitanja = 8;
                        int brojPitanja = 0;

                        while (brojPitanja < maxPitanja && igra.PostaviSledecePitanje())
                        {
                            msg = igra.TekucePitanje;
                            acceptSocket.Send(Encoding.UTF8.GetBytes(msg));

                            int br = acceptSocket.Receive(bufferRec);
                            string odgovor = Encoding.UTF8.GetString(bufferRec, 0, br).Trim().ToUpper();

                            Console.WriteLine($"Odgovor klijenta: {odgovor}");

                            if (odgovor == "KRAJ" || odgovor == "ODUSTAJEM")
                            {
                                acceptSocket.Send(Encoding.UTF8.GetBytes(
                                    $"KRAJ KVIZA! Poeni: {igrac.brojPoenaTrenutno}"
                                ));
                                break;
                            }

                            try
                            {
                                if (igra.ProveriOdgovor(odgovor))
                                {
                                    igrac.brojPoenaTrenutno += 4;
                                    msg = "TAČNO! +4 poena";
                                }
                                else
                                {
                                    msg = $"NETAČNO! Tačan odgovor je {(igra.TacanOdgovor ? "DA" : "NE")}";
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                msg = ex.Message;
                            }

                            acceptSocket.Send(Encoding.UTF8.GetBytes(msg));
                            brojPitanja++;
                            Thread.Sleep(500);
                        }

                        acceptSocket.Send(Encoding.UTF8.GetBytes(
                            $"KRAJ Pitanja I Odgovora! Ukupno poeni: {igrac.brojPoenaTrenutno}"
                        ));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("GRESKA U Pitanjima_I_Odgovorima IGRI: " + e.Message);
                    }

                    igrac.SacuvajPoene(i);
                }
                else if (nazIgre == "as")
                {
                    try
                    {
                        msg = "ASOCIJACIJE";
                        bufferSent = Encoding.UTF8.GetBytes(msg);
                        acceptSocket.Send(bufferSent);

                        string putanja = Path.Combine(baseDir, "FajloviZaIgre", "Asocijacije", "asocijacije1.txt");
                        Klase.Asocijacije.IgraAsocijacije igraAs = new Klase.Asocijacije.IgraAsocijacije();
                        igraAs.UcitajAsocijaciju(putanja);

                        bool igraTraje = true;
                        while (igraTraje)
                        {
                            msg = igraAs.PrikaziStanje();
                            acceptSocket.Send(Encoding.UTF8.GetBytes(msg));

                            int bytesRecOdgovor = acceptSocket.Receive(bufferRec);
                            string odgovor = Encoding.UTF8.GetString(bufferRec, 0, bytesRecOdgovor).Trim();

                            if (odgovor.Equals("ODUSTAJEM", StringComparison.OrdinalIgnoreCase) ||
                                odgovor.Equals("KRAJ", StringComparison.OrdinalIgnoreCase))
                            {
                                igrac.brojPoenaTrenutno += igraAs.GetPoeni();

                                acceptSocket.Send(Encoding.UTF8.GetBytes(igraAs.PrikaziStanje()));

                                acceptSocket.Send(Encoding.UTF8.GetBytes(
                                    $"KRAJ ASOCIJACIJA! Ukupno poeni: {igrac.brojPoenaTrenutno}"
                                ));

                                break;
                            }

                            bool validno = false;
                            bool kraj = false;
                            string statusPoruka = "";

                            if (odgovor.Length == 2)
                            {
                                validno = igraAs.OtvoriPolje(odgovor);
                                statusPoruka = validno ? "Otvoreno polje." : "Neispravan unos. Pokušajte ponovo.";
                            }
                            else if (odgovor.Contains(":") && !odgovor.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                            {
                                validno = igraAs.PogodiKolonu(odgovor);
                                statusPoruka = validno ? "Tačno! Pogodili ste kolonu." : "Netačno rešenje kolone.";
                            }
                            else if (odgovor.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                            {
                                validno = igraAs.PogodiKonacno(odgovor, out kraj);

                                statusPoruka = validno
                                    ? (kraj ? "Tačno! Pogodili ste konačno rešenje." : "Tačno.")
                                    : "Netačno konačno rešenje.";
                            }
                            else
                            {
                                validno = false;
                                statusPoruka = "Neispravan format unosa.";
                            }

                            igrac.brojPoenaTrenutno = igraAs.GetPoeni();

                            if (odgovor.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                            {
                                acceptSocket.Send(Encoding.UTF8.GetBytes(igraAs.PrikaziStanje()));

                                if (validno && kraj)
                                {
                                    acceptSocket.Send(Encoding.UTF8.GetBytes(
                                        $"Čestitamo! {statusPoruka} Ukupno poeni: {igrac.brojPoenaTrenutno}"
                                    ));
                                    break;
                                }

                                acceptSocket.Send(Encoding.UTF8.GetBytes(
                                    $"{statusPoruka} Poeni trenutno: {igrac.brojPoenaTrenutno}"
                                ));
                                continue;
                            }

                            acceptSocket.Send(Encoding.UTF8.GetBytes(
                                validno ? $"Poeni trenutno: {igrac.brojPoenaTrenutno}" : statusPoruka
                            ));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("GRESKA U ASOCIJACIJAMA: " + e.Message);
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

                    Thread.Sleep(300);
                    Console.Clear();

                    var lista = new List<Socket>(mapa.Keys);

                    var ulaganje = new UlaganjeKviska(mapa[lista[0]], mapa[lista[1]]);


                    ulaganje.UloziKviska(lista, 0);

                    anagramDvaIgraca(lista, mapa);

                    ulaganje.UloziKviska(lista, 1);

                    pioDvaIgraca(lista, mapa);

                    ulaganje.UloziKviska(lista, 2);

                    asocijacijeDvaIgraca(lista, mapa);

                    foreach (var s in lista)
                    {
                        try { s.Send(Encoding.UTF8.GetBytes("KRAJ_KVIZA")); } catch { }
                        try { s.Shutdown(SocketShutdown.Both); } catch { }
                        try { s.Close(); } catch { }
                    }

                    try { serverSocket.Close(); } catch { }
                    Console.WriteLine("SERVER: Kviz zavrsen.");
                    return;
                }

                Thread.Sleep(50);
            }
        }

        static void anagramDvaIgraca(List<Socket> klijenti, Dictionary<Socket, Igrac> mapa)
        {
            string putanja = Path.Combine(baseDir, "FajloviZaIgre", "Anagrami", "anagram2.txt");

            Dictionary<Socket, Anagram> anBySock = new Dictionary<Socket, Anagram>();
            foreach (var s in klijenti)
                anBySock[s] = new Anagram(putanja);

            string glavna = anBySock[klijenti[0]].REC;

            foreach (var s in klijenti) s.Send(Encoding.UTF8.GetBytes("ANAGRAM"));
            Thread.Sleep(10);
            foreach (var s in klijenti) s.Send(Encoding.UTF8.GetBytes(glavna));

            Console.WriteLine("=== MULTI ANAGRAM ===");
            //Console.WriteLine("Glavna rec: " + glavna);

            byte[] buf = new byte[1024];

            int ukupno = anBySock[klijenti[0]].ponudjeneReci.Count;

            Dictionary<string, Socket> prviPogodio = new Dictionary<string, Socket>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> brojBodovanja = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int pogodjeneBarJednom = 0;

            Dictionary<Socket, bool> zavrsio = new Dictionary<Socket, bool>();
            foreach (var s in klijenti) zavrsio[s] = false;

            while (true)
            {
                if (pogodjeneBarJednom >= ukupno)
                {
                    foreach (var s in klijenti)
                        s.Send(Encoding.UTF8.GetBytes("Sve reci su pogodjene. Kraj anagrama."));
                    break;
                }

                if (zavrsio.Values.All(x => x))
                {
                    foreach (var s in klijenti)
                        s.Send(Encoding.UTF8.GetBytes("Oba igraca su zavrsila. Kraj anagrama."));
                    break;
                }

                List<Socket> aktivni = new List<Socket>();
                foreach (var s in klijenti)
                    if (!zavrsio[s]) aktivni.Add(s);

                if (aktivni.Count == 0) break;

                Socket.Select(aktivni, null, null, 500 * 1000);

                foreach (var s in aktivni)
                {
                    int n;
                    try { n = s.Receive(buf); }
                    catch { n = 0; }

                    if (n == 0)
                    {
                        zavrsio[s] = true;
                        foreach (var other in klijenti)
                            if (other != s && !zavrsio[other])
                                other.Send(Encoding.UTF8.GetBytes($"{mapa[s].nickname} se diskonektovao"));
                        continue;
                    }

                    string rec = Encoding.UTF8.GetString(buf, 0, n).Trim();
                    if (string.IsNullOrWhiteSpace(rec)) continue;

                    Igrac ig = mapa[s];
                    //Console.WriteLine($"{ig.nickname}: {rec}");

                    if (rec.Equals("KRAJ", StringComparison.OrdinalIgnoreCase) ||
                        rec.Equals("ODUSTAJEM", StringComparison.OrdinalIgnoreCase))
                    {
                        zavrsio[s] = true;
                        s.Send(Encoding.UTF8.GetBytes($"Odustali ste. Vaši poeni: {ig.brojPoenaTrenutno}"));

                        foreach (var other in klijenti)
                            if (other != s && !zavrsio[other])
                                other.Send(Encoding.UTF8.GetBytes($"Protivnik ({ig.nickname}) je odustao. Vi igrate do kraja."));
                        continue;
                    }

                    var rez = anBySock[s].PostojiRec(rec, ig.id);

                    string key = rec.Trim().ToUpperInvariant();
                    if (!brojBodovanja.ContainsKey(key)) brojBodovanja[key] = 0;

                    if (rez == PovratneVrednostiAnagrama.IspravnaRec)
                    {
                        int poeni = key.Replace(" ", "").Length;

                        if (brojBodovanja[key] == 0)
                        {
                            brojBodovanja[key] = 1;
                            prviPogodio[key] = s;
                            pogodjeneBarJednom++;

                            ig.DodajPoene(poeni, 0);
                            s.Send(Encoding.UTF8.GetBytes($"TACNO! +{poeni} (prvi). {pogodjeneBarJednom}/{ukupno}"));
                        }
                        else if (brojBodovanja[key] == 1 && prviPogodio[key] != s)
                        {
                            int poeni2 = (int)Math.Floor(poeni * 0.85);
                            brojBodovanja[key] = 2;

                            ig.DodajPoene(poeni2, 0);
                            s.Send(Encoding.UTF8.GetBytes($"TACNO, ali kasnije! +{poeni2} (15% manje)."));
                        }
                        else
                        {
                            s.Send(Encoding.UTF8.GetBytes("Vec si dobio poene za ovu rec."));
                        }

                        continue;
                    }

                    if (rez == PovratneVrednostiAnagrama.NePostojiSlovo)
                        s.Send(Encoding.UTF8.GetBytes("NETACNO! Uneli ste slovo koje ne postoji u glavnoj reci."));
                    else if (rez == PovratneVrednostiAnagrama.PreviseSlova)
                        s.Send(Encoding.UTF8.GetBytes("NETACNO! Previše puta ste upotrebili neko slovo."));
                    else if (rez == PovratneVrednostiAnagrama.NeispravnaRec)
                        s.Send(Encoding.UTF8.GetBytes("NETACNO! Ta rec nije predlozena za ovaj anagram."));
                    else
                        s.Send(Encoding.UTF8.GetBytes("NETACNO!"));
                }

                Thread.Sleep(20);
            }

            foreach (var s in klijenti)
            {
                try
                {
                    var ig = mapa[s];
                    s.Send(Encoding.UTF8.GetBytes($"KRAJ ANAGRAMA! Ukupno poeni: {ig.brojPoenaTrenutno}"));
                }
                catch { }
            }

            /*
            foreach (var s in klijenti)
            {
                try { s.Shutdown(SocketShutdown.Both); } catch { }
                try { s.Close(); } catch { }
            }
            */
        }

        static void pioDvaIgraca(List<Socket> klijenti, Dictionary<Socket, Igrac> mapa)
        {
            string putanja = Path.Combine(baseDir, "FajloviZaIgre", "PiO", "pitanja.txt");

            Dictionary<Socket, PitanjaOdgovori> pioBySock = new Dictionary<Socket, PitanjaOdgovori>();
            foreach (var s in klijenti)
                pioBySock[s] = new PitanjaOdgovori(putanja);

            foreach (var s in klijenti) s.Send(Encoding.UTF8.GetBytes("PITANJA_IODGOVORI"));

            Console.WriteLine("=== MULTI PITANJA I ODGOVORI ===");

            byte[] buf = new byte[1024];
            int maxPitanja = 8;

            Dictionary<Socket, int> brojPitanja = new Dictionary<Socket, int>();
            Dictionary<Socket, bool> zavrsio = new Dictionary<Socket, bool>();
            Dictionary<Socket, bool> cekaRezultat = new Dictionary<Socket, bool>();

            foreach (var s in klijenti)
            {
                brojPitanja[s] = 0;
                zavrsio[s] = false;
                cekaRezultat[s] = false;

                if (pioBySock[s].PostaviSledecePitanje())
                {
                    s.Send(Encoding.UTF8.GetBytes($"PITANJE|{pioBySock[s].TekucePitanje}"));
                    brojPitanja[s]++;
                }
            }

            while (true)
            {
                if (zavrsio.Values.All(x => x))
                {
                    break;
                }

                List<Socket> aktivni = new List<Socket>();
                foreach (var s in klijenti)
                    if (!zavrsio[s]) aktivni.Add(s);

                if (aktivni.Count == 0) break;

                Socket.Select(aktivni, null, null, 500 * 1000);

                foreach (var s in aktivni)
                {
                    int n;
                    try { n = s.Receive(buf); }
                    catch { n = 0; }

                    if (n == 0)
                    {
                        zavrsio[s] = true;
                        continue;
                    }

                    string odgovor = Encoding.UTF8.GetString(buf, 0, n).Trim().ToUpper();
                    Igrac ig = mapa[s];

                    if (odgovor == "KRAJ" || odgovor == "ODUSTAJEM")
                    {
                        zavrsio[s] = true;
                        s.Send(Encoding.UTF8.GetBytes($"REZULTAT|Odustali ste. Vaši poeni: {ig.brojPoenaTrenutno}"));

                        foreach (var other in klijenti)
                            if (other != s && !zavrsio[other])
                                other.Send(Encoding.UTF8.GetBytes($"REZULTAT|Protivnik ({ig.nickname}) je odustao. Vi igrate do kraja."));
                        continue;
                    }

                    string ishod = "";
                    if (pioBySock[s].ProveriOdgovor(odgovor))
                    {
                        ig.DodajPoene(4, 1);
                        ishod = "REZULTAT|TAČNO! +4 poena";
                    }
                    else
                    {
                        string tacan = pioBySock[s].TacanOdgovor ? "DA" : "NE";
                        ishod = $"REZULTAT|NETAČNO! Tačan odgovor je bio {tacan}";
                    }

                    s.Send(Encoding.UTF8.GetBytes(ishod));

                    if (brojPitanja[s] < maxPitanja && pioBySock[s].PostaviSledecePitanje())
                    {
                        Thread.Sleep(200);
                        s.Send(Encoding.UTF8.GetBytes($"PITANJE|{pioBySock[s].TekucePitanje}"));
                        brojPitanja[s]++;
                    }
                    else
                    {
                        zavrsio[s] = true;
                        s.Send(Encoding.UTF8.GetBytes($"REZULTAT|Završili ste svih {maxPitanja} pitanja."));
                    }
                }
            }

            foreach (var s in klijenti)
            {
                try
                {
                    Thread.Sleep(10);
                    string finalMsg = $"KRAJ_PIO|Kraj Pitanja I Odgovora! Ukupno poena: {mapa[s].brojPoenaTrenutno}";
                    s.Send(Encoding.UTF8.GetBytes(finalMsg));
                }
                catch { }
            }

            // Sinhronizacija
            /*foreach (var s in klijenti)
            {
                try { s.Receive(buf); } catch { }
            }*/
        }

        static void asocijacijeDvaIgraca(List<Socket> klijenti, Dictionary<Socket, Igrac> mapa)
        {
            string putanja = Path.Combine(baseDir, "FajloviZaIgre", "Asocijacije", "asocijacije1.txt");

            IgraAsocijacije igraAs = new IgraAsocijacije();
            igraAs.UcitajAsocijaciju(putanja);

            void SendLine(Socket sock, string text)
            {
                try { sock.Send(Encoding.UTF8.GetBytes(text + "\n")); } catch { }
            }

            foreach (var s in klijenti) SendLine(s, "ASOCIJACIJE");

            Console.WriteLine("=== MULTI ASOCIJACIJE ===");

            int current = 0;
            byte[] buf = new byte[4096];

            Dictionary<Socket, int> poeniAs = new Dictionary<Socket, int>();
            foreach (var s in klijenti) poeniAs[s] = 0;

            int prevTotal = igraAs.GetPoeni();

            while (true)
            {
                string stanje = igraAs.PrikaziStanje()
                    .Replace("\r", "")
                    .Replace("\n", "\\n");
                foreach (var s in klijenti) SendLine(s, "STANJE|" + stanje);

                Socket naPotezu = klijenti[current];
                Socket ceka = klijenti[1 - current];

                SendLine(naPotezu, "TVOJ_POTEZ|Unesite potez (A1, A:resenje, K:resenje, KRAJ/ODUSTAJEM):");
                SendLine(ceka, $"CEKAJ|Na potezu je {mapa[naPotezu].nickname}.");

                int n = 0;
                try
                {
                    if (!naPotezu.Poll(120 * 1000 * 1000, SelectMode.SelectRead))
                    {
                        n = 0;
                    }
                    else
                    {
                        n = naPotezu.Receive(buf);
                    }
                }
                catch
                {
                    n = 0;
                }

                if (n == 0)
                {
                    string msg = $"KRAJ_AS|Igrac {mapa[naPotezu].nickname} je odustao ili se diskonektovao." +
                                 $"As poeni: {mapa[klijenti[0]].nickname}={poeniAs[klijenti[0]]}, {mapa[klijenti[1]].nickname}={poeniAs[klijenti[1]]}.";
                    foreach (var s in klijenti) SendLine(s, msg);
                    return;
                }

                string potez = Encoding.UTF8.GetString(buf, 0, n).Trim();
                if (string.IsNullOrWhiteSpace(potez))
                {
                    foreach (var s in klijenti) SendLine(s, "REZULTAT|Neispravan unos (prazno).");
                    continue;
                }

                bool validno = false;
                bool kraj = false;
                string rezultatTekst = "";

                if (potez.Equals("KRAJ", StringComparison.OrdinalIgnoreCase) ||
                    potez.Equals("ODUSTAJEM", StringComparison.OrdinalIgnoreCase))
                {
                    string msg =
                        $"KRAJ_AS|Kraj Asocijacija! As poeni: {mapa[klijenti[0]].nickname}={poeniAs[klijenti[0]]}, " +
                        $"{mapa[klijenti[1]].nickname}={poeniAs[klijenti[1]]}. Ukupno poena (kroz kviz): " +
                        $"{mapa[klijenti[0]].nickname}={mapa[klijenti[0]].brojPoenaTrenutno}, {mapa[klijenti[1]].nickname}={mapa[klijenti[1]].brojPoenaTrenutno}.";
                    foreach (var s in klijenti) SendLine(s, msg);
                    return;
                }

                if (potez.Length == 2)
                {
                    validno = igraAs.OtvoriPolje(potez);
                    rezultatTekst = validno ? "Otvoreno polje." : "Neispravan potez (polje).";
                }
                else if (potez.Contains(":") && !potez.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                {
                    validno = igraAs.PogodiKolonu(potez);
                    rezultatTekst = validno ? "Tačno! Pogodjena kolona." : "Netačno rešenje kolone.";
                }
                else if (potez.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                {
                    validno = igraAs.PogodiKonacno(potez, out kraj);
                    rezultatTekst = validno ? (kraj ? "Tačno! Pogodjeno konačno rešenje." : "Tačno.") : "Netačno konačno rešenje.";
                }
                else
                {
                    validno = false;
                    rezultatTekst = "Neispravan format poteza.";
                }

                int afterTotal = igraAs.GetPoeni();
                int gained = Math.Max(0, afterTotal - prevTotal);
                prevTotal = afterTotal;

                if (gained > 0)
                {
                    int primenjeni = gained * (mapa[naPotezu].IndexKvisko == 2 ? 2 : 1);
                    poeniAs[naPotezu] += primenjeni;
                    mapa[naPotezu].DodajPoene(gained, 2);
                }

                string rez = $"REZULTAT|{mapa[naPotezu].nickname}: {rezultatTekst}" +
                             (gained > 0 ? $" (+{(mapa[naPotezu].IndexKvisko == 2 ? gained * 2 : gained)} poena)" : "");
                foreach (var s in klijenti) SendLine(s, rez);

                if (kraj && validno)
                {
                    string krajMsg =
                        $"KRAJ_AS|Kraj Asocijacija! As poeni: {mapa[klijenti[0]].nickname}={poeniAs[klijenti[0]]}, " +
                        $"{mapa[klijenti[1]].nickname}={poeniAs[klijenti[1]]}. Ukupno poena (kroz kviz): " +
                        $"{mapa[klijenti[0]].nickname}={mapa[klijenti[0]].brojPoenaTrenutno}, {mapa[klijenti[1]].nickname}={mapa[klijenti[1]].brojPoenaTrenutno}.";
                    foreach (var s in klijenti) SendLine(s, krajMsg);
                    return;
                }

                current = 1 - current;
            }
        }


    }
}
