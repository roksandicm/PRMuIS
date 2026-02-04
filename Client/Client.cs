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

            if (string.IsNullOrWhiteSpace(multiplayerToken))
            {
                Console.WriteLine("Greska: nije primljen token.");
                TCPclientSocket.Close();
                return;
            }

            string tokenMsg = "token," + multiplayerToken;
            TCPclientSocket.Send(Encoding.UTF8.GetBytes(tokenMsg));

            byte[] buffer = new byte[1024];
            int br = TCPclientSocket.Receive(buffer);
            string resp = Encoding.UTF8.GetString(buffer, 0, br).Trim();
            if (resp != "ok_token")
            {
                Console.WriteLine("Server je odbio token: " + resp);
                TCPclientSocket.Close();
                return;
            }

            string poruka = "";
            do
            {
                Console.Write("Unesite \"START\" za pocetak kviza: ");
                poruka = Console.ReadLine();
            } while (poruka.ToLower() != "start");

            TCPclientSocket.Send(Encoding.UTF8.GetBytes(poruka));

            br = TCPclientSocket.Receive(buffer);
            poruka = Encoding.UTF8.GetString(buffer, 0, br);
            Console.WriteLine(poruka);
            Console.WriteLine();

            TCPclientSocket.Close();
        }
    }
}
