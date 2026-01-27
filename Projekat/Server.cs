using Klase;
using Klase.Igrac;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Projekat
{
    public class Server
    {

        private static Igrac igrac;
        private static List<Igrac> igraci = [];
        private static List<EndPoint> endPointIgraca = [];
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
                    if(deloviPoruke.Length == 1)
                    {
                        igraci.Add(new Igrac(deloviPoruke[0]));
                        endPointIgraca.Add(sender);


                        if(igraci.Count == 2)
                        {
                            poruka = "igra";
                            bafer = Encoding.UTF8.GetBytes(poruka);
                            foreach (EndPoint ep in endPointIgraca)
                            {
                                int bytesSent = udpSocket.SendTo(bafer, ep);
                            }
                            Console.WriteLine("\n-----------------------------------------------------------------");
                            Console.WriteLine("\t\t\tPOKRETANJE KVIZA");
                            Console.WriteLine("-----------------------------------------------------------------\n");
                            Thread.Sleep(1500);
                            Console.Clear();
                            igraDvaIgraca();
                            break;
                        }else
                        {
                            Console.WriteLine("Potreban je jos jedan igrac...");
                            continue;
                        }

                    }else if(deloviPoruke.Length > 1)
                    {
                        igrac = new Igrac(deloviPoruke);
                        poruka = "trening";
                        bafer = Encoding.UTF8.GetBytes(poruka);
                        int bytesSent = udpSocket.SendTo(bafer, sender);
                        Console.WriteLine("\n-----------------------------------------------------------------");
                        Console.WriteLine("\t\t\tPOKRETANJE TRENINGA");
                        Console.WriteLine("-----------------------------------------------------------------\n");
                        Thread.Sleep(1500);
                        Console.Clear();
                        treningJedanIgrac();
                        break;
                    }

                }catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // ovde rezultati dalje vrv
        }

        static void treningJedanIgrac()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 51001);
            tcpSocket.Bind(ep);
            tcpSocket.Listen(10);
            byte[] bufferRec = new byte[1024];
            byte[] bufferSent = new byte[1024];
            Console.WriteLine("TCP server je poceo sa radom...");

            Socket acceptSocket = tcpSocket.Accept();
            Console.WriteLine($"Povezao se novi klijent {(acceptSocket.RemoteEndPoint as IPEndPoint)?.Address}");
            
            string msg = $"Dobrodošli u trening igru kviza Kviskoteka, današnji takmičar je {igrac.nickname}";
            bufferSent = Encoding.UTF8.GetBytes(msg);
            int bytesSent = acceptSocket.Send(bufferSent);

            int bytesRec = acceptSocket.Receive(bufferRec);
            msg = Encoding.UTF8.GetString(bufferRec, 0, bytesRec);

            msg = igrac.brojIgre.ToString();
            bufferSent = Encoding.UTF8.GetBytes(msg);
            acceptSocket.Send(bufferSent);

            for (int i = 0; i < igrac.brojIgre; i++)
            {
                string nazIgre = igrac.GetIgra(i);

                if(nazIgre == "an")
                {
                    msg = "ANAGRAM";
                }

                if (nazIgre == "po")
                {
                    msg = "PITANJA I ODGOVORI";
                }

                if(nazIgre == "as")
                {
                    msg = "ASOCIJACIJE";
                }
            }

            Console.WriteLine("\n-----------------------------------------------------------------\n");
            Console.WriteLine("\t\t\tPODACI O IGRACU");
            Console.WriteLine(igrac);
            Console.WriteLine("-----------------------------------------------------------------\n");

            //Thread.Sleep(3000);
            //Console.Clear();
        }

        static void igraDvaIgraca()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 51001);

            tcpSocket.Bind(ep);
            tcpSocket.Blocking = false;
            tcpSocket.Listen(2);
        }
    }
}
