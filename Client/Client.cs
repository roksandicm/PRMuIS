using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    internal class Client
    {
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

                    int bytesRec = udpSocket.ReceiveFrom(Recbuffer, ref recEP);
                    msg = Encoding.UTF8.GetString(Recbuffer, 0, bytesRec);

                    if (msg == "trening")
                    {
                        Console.WriteLine("Zapocinjanje treninga...");
                        Thread.Sleep(1000);
                        Console.Clear();
                        samoTrening();
                        break;
                    }
                    else if (msg == "igra")
                    {
                        Console.WriteLine("Zapocinjanje kviza...");
                        Thread.Sleep(1000);
                        Console.Clear();
                        kviz();
                        break;
                    }
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
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 51001);
            byte[] bufferRec = new byte[1024];
            byte[] bufferSent = new byte[1024];
            tcpSocket.Connect(ep);


            
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

            tcpSocket.Close();
        }

        static void kviz()
        {
            Console.WriteLine("Kviz jos nije implementiran.");
        }
    }
}
