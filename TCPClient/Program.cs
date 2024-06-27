using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

class TCPClient
{
    private const string SERVER_IP = "127.0.0.1";
    private const int PORT = 12345;
    private static int _connectionId = 0;
    private static TcpClient _client = new TcpClient(SERVER_IP, PORT);
    private static NetworkStream stream = _client.GetStream();

    static void Main(string[] args)
    {
         
        Console.WriteLine("Conectado");
        while (_client.Connected)
        {
            ListenForServerMessages();

            ListenConsole();
        }

    }

    private static void ListenConsole()
    {
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        if (Console.KeyAvailable)
        {

            var message = Console.ReadLine();


            if (message != null)
            {
                if (message.ToLower() == "sair")
                {
                    writer.WriteLine(message);

                    _client.Close();
                }

                if (message.StartsWith("File"))
                {
                    writer.WriteLine(message);

                    var fileName = message.Substring(7).Trim();
                    ReceiveFile(fileName);
                }

                if (message.StartsWith("Chat"))
                {
                    writer.WriteLine(message);
                }

            }

        }
    }

    private static void ListenForServerMessages()
    {
        var stream = _client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            var teste = stream.Socket.Available;

            if (teste > 3)
            {

                var message = reader.ReadLine();

                if (message == null) return;

                message = message.Replace("\uFEFF", "");

                if (message.StartsWith("Connection"))
                {
                    _connectionId = int.Parse(message.Substring(11).Trim());
                }

                if (message.StartsWith("Arquivo")){
                    var fileName = message.Substring(7).Trim();
                    ReceiveFile(fileName);
                }
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
    }

    private static void ReceiveFile(string fileName)
    {
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        var status = reader.ReadLine();
        Console.WriteLine(status);


        if (status == "Status: nok")
        {
            status = reader.ReadLine();
            Console.WriteLine(status);
            return;
        }
        
        var name = reader.ReadLine().Substring(6);
        Console.WriteLine(name);

        var sizeString = reader.ReadLine().Substring(9);
        Console.WriteLine(sizeString);

        if (!long.TryParse(sizeString, out long size))
        {
            Console.WriteLine("Erro ao converter o tamanho do arquivo.");
            return;
        }
        var hash = reader.ReadLine().Substring(6);

        Console.WriteLine("Hash: " + hash);

        using (var fileStream = new FileStream($"received_Client_{_connectionId}_{name}", FileMode.Create, FileAccess.Write))
        {
            byte[] buffer = new byte[20*1024]; // 20k Budder
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (buffer[0] == 0x00 && bytesRead == 1)
                {
                    Console.WriteLine("Fim da transmissão detectado.");
                    break;
                }
                fileStream.Write(buffer, 0, bytesRead);
                //Console.WriteLine($"Recebido {bytesRead} bytes");
            }

            fileStream.Close();

        }

        using (var sha256 = SHA256.Create())
        {
            using (var fileStream = File.OpenRead($"received_Client_{_connectionId}_{name}"))
            {
                var computedHash = sha256.ComputeHash(fileStream);
                var computedHashString = BitConverter.ToString(computedHash).Replace("-", "").ToLower();
                if (computedHashString == hash)
                {
                    Console.WriteLine("Arquivo recebido com sucesso e verificado.");
                }
                else
                {
                    Console.WriteLine("Erro de integridade no arquivo recebido.");
                }
            }
        }
    }
}
