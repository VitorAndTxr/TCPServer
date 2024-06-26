using System.Net;
using System.Net.Sockets;


class TCPServer
{
    private const int PORT = 12345;
    public static int id = 0;
    private static List<TCPClientHandler> clients = new List<TCPClientHandler>();
    private static TcpListener listener = OpenSocket();
    private static List<string> broadcastServerMessages = new List<string>();

    static void Main(string[] args)
    {
        MainLoop();
    }

    private static void MainLoop()
    {
        while (true)
        {
            FindNewClients();
            SendBroadCast();
        }
    }

    private static void SendBroadCast()
    {
        if(broadcastServerMessages.Count >0){

            foreach(var client in clients){

                lock(broadcastServerMessages){

                    foreach(var broadcastServerMessage in broadcastServerMessages){

                        client.SendBroadCast(broadcastServerMessage);
                    }
                }
            }
        }
    }

    private static void FindNewClients()
    {
        var client = listener?.AcceptTcpClient();

        if (client != null)
        {
            if (client != null)
            {

                if(clients.Count == 0 ){
                    id = 0;
                }

                id++;

                var clientHandler = new TCPClientHandler(client, id);

                var clientThread = new Thread(clientHandler.HandleClient);

                clientThread.Start();

                lock (clients)
                {
                    clients.Add(clientHandler);
                }

                Console.WriteLine($"Cliente {id} conectado.");

            }
        }
    }

    private static TcpListener OpenSocket()
    {
        listener = new TcpListener(IPAddress.Any, PORT);
        listener.Start();
        Console.WriteLine($"Servidor iniciado na porta {PORT}. Aguardando conexões...");

        return listener;
    }
 
}
