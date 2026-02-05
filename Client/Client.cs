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
            byte[] bufferRec = new byte[1024];
            byte[] bufferSent = new byte[1024];
            tcpSocket.Connect(ep);

            try
            {
                int recByte = tcpSocket.Receive(bufferRec);
                string msg = Encoding.UTF8.GetString(bufferRec, 0, recByte);
                Console.WriteLine(msg);

                string zaStart = "";
                while (zaStart.ToUpper() != "START")
                {
                    Console.Write("Unesite 'START' za pocetak: ");
                    zaStart = Console.ReadLine();
                }
                bufferSent = Encoding.UTF8.GetBytes(zaStart);
                tcpSocket.Send(bufferSent);

                recByte = tcpSocket.Receive(bufferRec);
                int brIgara = int.Parse(Encoding.UTF8.GetString(bufferRec, 0, recByte));

                for (int i = 0; i < brIgara; i++)
                {
                    recByte = tcpSocket.Receive(bufferRec);
                    msg = Encoding.UTF8.GetString(bufferRec, 0, recByte);
                    Console.WriteLine($"\nIgra {i + 1}: {msg}");

                    if (msg == "ANAGRAM")
                    {
                        recByte = tcpSocket.Receive(bufferRec);
                        string glavnaRec = Encoding.UTF8.GetString(bufferRec, 0, recByte);
                        Console.WriteLine("Glavna reč: " + glavnaRec);

                        bool igraTraje = true;
                        while (igraTraje)
                        {
                            Console.Write("Unesite odgovor (ili 'ODUSTAJEM'/ 'KRAJ'): ");
                            string odgovor = Console.ReadLine();
                            bufferSent = Encoding.UTF8.GetBytes(odgovor);
                            tcpSocket.Send(bufferSent);

                            recByte = tcpSocket.Receive(bufferRec);
                            msg = Encoding.UTF8.GetString(bufferRec, 0, recByte);
                            Console.WriteLine(msg);

                            if (msg.StartsWith("KRAJ ANAGRAMA") || msg.StartsWith("Odustali ste"))
                                igraTraje = false;
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

            byte[] buffer = new byte[2048];

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

            for (int game = 0; game < 3; game++)
            {
                br = TCPclientSocket.Receive(buffer);
                if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); TCPclientSocket.Close(); return; }

                string igra = Encoding.UTF8.GetString(buffer, 0, br).Trim();
                Console.WriteLine($"\n=== IGRA {game + 1}: {igra} ===\n");

                // ANAGRAM
                if (igra.Equals("ANAGRAM", StringComparison.OrdinalIgnoreCase))
                {
                    br = TCPclientSocket.Receive(buffer);
                    if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); TCPclientSocket.Close(); return; }

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
                            catch { Console.WriteLine("Pukla veza sa serverom."); TCPclientSocket.Close(); return; }

                            if (unos.Equals("kraj", StringComparison.OrdinalIgnoreCase) ||
                                unos.Equals("odustajem", StringComparison.OrdinalIgnoreCase))
                            {
                                jaZavrsio = true;
                                Console.WriteLine("(Zavrsio si. Cekas drugog igraca...)");
                            }
                        }

                        br = TCPclientSocket.Receive(buffer);
                        if (br == 0) { Console.WriteLine("Server zatvorio konekciju."); TCPclientSocket.Close(); return; }

                        string msg = Encoding.UTF8.GetString(buffer, 0, br).Trim();
                        Console.WriteLine(msg);

                        if (msg.StartsWith("KRAJ ANAGRAMA", StringComparison.OrdinalIgnoreCase))
                            break;
                    }

                    Console.WriteLine("\n--- Kraj ANAGRAMA ---\n");
                    continue;
                }

                if (igra.Equals("PITANJA", StringComparison.OrdinalIgnoreCase) ||
                    igra.Equals("PITANJA I ODGOVORI", StringComparison.OrdinalIgnoreCase) ||
                    igra.Equals("PO", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("PO nije implementiran u klijentu jos. (stub)");
                    continue;
                }

                if (igra.Equals("ASOCIJACIJE", StringComparison.OrdinalIgnoreCase) ||
                    igra.Equals("AS", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("AS nije implementiran u klijentu jos. (stub)");
                    continue;
                }

                Console.WriteLine("Nepoznata igra: " + igra);
            }

            Console.WriteLine("\nKviz zavrsen!\n");
            TCPclientSocket.Close();
        }




    }
}
