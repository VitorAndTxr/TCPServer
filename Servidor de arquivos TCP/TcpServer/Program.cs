using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

class TCPServer
{
    private static TcpListener listener;
    private const int PORT = 12345;

    static void Main(string[] args)
    {
        listener = new TcpListener(IPAddress.Any, PORT);
        listener.Start();
        Console.WriteLine($"Servidor iniciado na porta {PORT}. Aguardando conexões...");

        while (true)
        {
            var client = listener.AcceptTcpClient();
            Console.WriteLine("Cliente conectado.");
            var clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            while (true)
            {
                var message = reader.ReadLine();
                if (message == null) break;

                if (message.ToLower() == "sair")
                {
                    Console.WriteLine("Cliente desconectado.");
                    break;
                }
                else if (message.StartsWith("Arquivo"))
                {
                    var fileName = message.Substring(7).Trim();
                    SendFile(fileName, writer, stream);
                }
                else if (message.StartsWith("Chat"))
                {
                    var chatMessage = message.Substring(4).Trim();
                    Console.WriteLine($"Cliente: {chatMessage}");
                    writer.WriteLine($"Servidor: {chatMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private static void SendFile(string fileName, StreamWriter writer, NetworkStream stream)
    {
        if (!File.Exists(fileName))
        {
            writer.WriteLine("Status: nok");
            writer.WriteLine("Arquivo inexistente.");
            return;
        }


        writer.WriteLine("Status: ok");
        Console.WriteLine("Status: ok");

        var fileInfo = new FileInfo(fileName);
        writer.WriteLine($"Nome: {fileInfo.Name}");
        Console.WriteLine($"Nome: {fileInfo.Name}");
        
        writer.WriteLine($"Tamanho: {fileInfo.Length}");
        Console.WriteLine($"Tamanho: {fileInfo.Length}");


        using (var sha256 = SHA256.Create())
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                var hash = sha256.ComputeHash(fileStream);
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                writer.WriteLine($"Hash: {hashString}");
            }
        }

        using (var fileStream = File.OpenRead(fileName))
        {
            fileStream.CopyTo(stream);
        }
    }
}
