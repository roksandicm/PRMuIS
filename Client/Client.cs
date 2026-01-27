using System.Net;
using System.Net.Sockets;
using System.Text;

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
            byte[] Sendbuffer = new byte[1024];
            byte[] Recbuffer = new byte[1024];

            while (true)
            {

                Console.Write("Prijava: ");
                string msg = Console.ReadLine();
                Sendbuffer = Encoding.UTF8.GetBytes(msg);

                try
                {
                    int bytesSent = udpSocket.SendTo(Sendbuffer, targetEP);

                    int bytesRec = udpSocket.ReceiveFrom(Recbuffer, ref recEP);
                    msg = Encoding.UTF8.GetString(Recbuffer, 0, bytesRec);

                    if (msg == "trening")
                    {
                        Console.WriteLine("Zapocinjanje treninga...");
                        Thread.Sleep(1500);
                        Console.Clear();
                        samoTrening();
                        break;
                    }
                    else if (msg == "igra")
                    {
                        Console.WriteLine("Zapocinjanje kviza...");
                        Thread.Sleep(1500);
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

            Console.WriteLine("Gasenje UDP klijenta...");
            Thread.Sleep(2000);
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

            string zaStart = string.Empty;
            while (zaStart.ToUpper() != "START")
            {
                Console.Write("Unesite \"START\" za pocetak: ");
                zaStart = Console.ReadLine();
            }
            bufferSent = Encoding.UTF8.GetBytes(zaStart);
            int bytesSent = tcpSocket.Send(bufferSent);

            recByte = tcpSocket.Receive(bufferRec);
            msg = Encoding.UTF8.GetString(bufferRec, 0, recByte);
            int brIgara = int.Parse(msg);
            Console.WriteLine(brIgara);
            for (int i = 0; i < brIgara; i++)
            {

            }
        }

        static void kviz()
        {
            
        }

    }
}
