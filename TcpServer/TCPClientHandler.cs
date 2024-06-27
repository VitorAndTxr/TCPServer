using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

public class TCPClientHandler
{
    private TcpClient _client;
    private int _connectionId;

    public TCPClientHandler(TcpClient client, int connectionId)
    {
        _client = client;
        _connectionId = connectionId;

    }

    public void SendBroadCast(string message)
    {
        var _stream = _client.GetStream();
        StreamWriter writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
        writer.WriteLine($"Mensagem do servidor: {message}");
        Console.WriteLine($"BroadcastedMessage in Client {_connectionId}: {message}");
    }

    public void HandleClient()
    {
        try
        {
            var _stream = _client.GetStream();

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            StreamWriter writer = new StreamWriter(_stream, encoding) { AutoFlush = true };
            writer.WriteLine($"Connection:{_connectionId}");
            Console.WriteLine($"HandleClient started for Client {_connectionId}");

            var ListenSocketTask = ListenSocket();
            Task.WhenAll(ListenSocketTask).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleClient for Client {_connectionId}: {ex.Message}");
        }
        finally
        {
            _client.Close();
            Console.WriteLine($"Client {_connectionId} disconnected (finally block).");
        }
    }

    private async Task ListenSocket()
    {
        var _stream = _client.GetStream();

        var encoding = new UTF8Encoding();
        StreamReader reader = new StreamReader(_stream, encoding);
        StreamWriter writer = new StreamWriter(_stream, encoding) { AutoFlush = true };

        while (_client.Connected)
        {


            var teste = _stream.Socket.Available;

            if (teste > 3)
            {

                var message = await reader.ReadLineAsync();
                if (message == null)
                {
                    Console.WriteLine($"Client {_connectionId} disconnected.");
                    break;
                }
                message = message.Replace("\uFEFF", "");

                Console.WriteLine($"Message received from Client {_connectionId}: {message}");

                if (message.ToLower() == "sair")
                {
                    Console.WriteLine($"Client {_connectionId} requested disconnect.");
                    break;
                }
                else if (message.StartsWith("File"))
                {
                    var fileName = message.Substring(4).Trim();
                    SendFile(fileName);
                }
                else if (message.StartsWith("Chat"))
                {
                    var chatMessage = message.Replace("\uFEFF","").Substring(4).Trim();
                    Console.WriteLine($"Client {_connectionId}: {chatMessage}");
                    writer.WriteLine($"ACKChat: {chatMessage}");
                }
            }

        }

    }

    private void SendFile(string fileName)
    {
        try
        {
            var _stream = _client.GetStream();

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            StreamReader reader = new StreamReader(_stream, encoding);
            StreamWriter writer = new StreamWriter(_stream, encoding) { AutoFlush = true };

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
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var hash = sha256.ComputeHash(fileStream);
                    var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    writer.WriteLine($"Hash: {hashString}");
                    Console.WriteLine($"Sending file to Client {_connectionId}: {fileName}");
                    byte[] buffer = new byte[20 * 1024]; // 1 MB buffer
                    int bytesRead;

                    fileStream.Position = 0;

                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)                
                    {
                        _stream.Write(buffer, 0, bytesRead);
                        //Console.WriteLine($"Enviado {bytesRead} bytes");
                    }
                    _stream.WriteByte(0x00);
                    Console.WriteLine("Fim da transmissão.");
                }

                Console.WriteLine($"File sent successfully to Client {_connectionId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending file to Client {_connectionId}: {ex.Message}");
        }
    }
}