using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class SimpleHttpServer
{
    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine("Request received:\n" + request);

        string response = "";

        if (request.StartsWith("GET /pagina.html"))
        {
            response = "HTTP/1.0 200 OK\r\n" +
                              "Content-Type: text/html; charset=UTF-8\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              "\r\n" +
                              "<html><body><h1>Hello, World!</h1></body></html>";

        }
        else
        {
            response = "HTTP/1.0 404 Not Found\r\n" +
                                  "Content-Type: text/html; charset=UTF-8\r\n" +
                                  "Access-Control-Allow-Origin: *\r\n" +
                                  "\r\n" +
                                  "<html><body><h1>404 Not Found</h1></body></html>";

        }

        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        stream.Write(responseBytes, 0, responseBytes.Length);

        client.Close();
    }

    public static void Main()
    {
        int port = 8081; // Porta do servidor
        TcpListener server = new TcpListener(IPAddress.Any, port);

        server.Start();
        Console.WriteLine("Server started on port " + port);

        while (true)
        {
            Console.WriteLine("Waiting for a connection...");
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!");

            Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
            clientThread.Start(client);
        }
    }
}
