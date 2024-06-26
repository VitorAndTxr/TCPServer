using System.Net.Http;
using System.Net.Sockets;
using System.Net;

class TCPServer
{
    private const int PORT = 12345;
    public static int id = 0;
    private static List<TCPClientHandler> clients = new List<TCPClientHandler>();
    private static TcpListener listener = OpenSocket();

    static void Main(string[] args)
    {
        MainLoop();
    }

    private static void MainLoop()
    {
        listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        while (true)
        {
            SendBroadCast();
        }
    }

    private static void SendBroadCast()
    {

        if (Console.KeyAvailable)
        {
            var message = Console.ReadLine();

            foreach (var client in clients)
            {

                client.SendBroadCast(message);

            }

        }
    }

    private static void OnClientConnect(IAsyncResult ar)
    {
        TcpClient client = listener.EndAcceptTcpClient(ar);

        if (client != null)
        {


            if (clients.Count == 0)
            {
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

            listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);

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
