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

    static void Main(string[] args)
    {
        var client = new TcpClient(SERVER_IP, PORT);
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        while (true)
        {
            var message = Console.ReadLine();

            if(message!= null){
                if (message.ToLower() == "sair")
                {
                    break;
                }
                else if (message.StartsWith("Arquivo"))
                {
                    writer.WriteLine(message);

                    var fileName = message.Substring(7).Trim();
                    ReceiveFile(fileName, writer, reader, stream);
                }

            }

        }

        client.Close();
    }

    private static void ListenForServerMessages(StreamReader reader, StreamWriter writer, NetworkStream stream)
    {
        try
        {
            while (true)
            {
                var message = reader.ReadLine();
                if (message == null) break;
                if(message.StartsWith("Arquivo")){
                    var fileName = message.Substring(7).Trim();
                    ReceiveFile(fileName, writer, reader, stream);
                }
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
    }

    private static void ReceiveFile(string fileName, StreamWriter writer, StreamReader reader, NetworkStream stream)
    {
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

        var sizeString = reader.ReadLine().Substring(8);
        Console.WriteLine(sizeString);

        if (!long.TryParse(sizeString, out long size))
        {
            Console.WriteLine("Erro ao converter o tamanho do arquivo.");
            return;
        }
        var hash = reader.ReadLine().Substring(6);

        Console.WriteLine("Hash: " + hash);

        var buffer = new byte[size];
        using (var fileStream = new FileStream($"received_{name}", FileMode.Create, FileAccess.Write))
        {
            long totalBytesRead = 0;
            while (totalBytesRead < size)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                fileStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
            }
        }

        using (var sha256 = SHA256.Create())
        {
            using (var fileStream = File.OpenRead($"received_{name}"))
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
