using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class TCPClientService
{
    private const string SERVER_IP = "127.0.0.1";
    private const int PORT = 12345;

    private static TcpClient _client;
    private static NetworkStream _stream;

    static async Task Main(string[] args)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(SERVER_IP, PORT);

            _stream = _client.GetStream();

            Console.WriteLine("Conectado ao servidor.");

            // Tarefas assíncronas para ouvir mensagens do servidor e enviar mensagens do console
            var listenTask = ListenForServerMessagesAsync();
            var sendTask = SendConsoleMessagesAsync();

            // Aguardar que ambas as tarefas sejam concluídas
            await Task.WhenAll(listenTask, sendTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
        finally
        {
            _client.Close();
            Console.WriteLine("Conexão fechada.");
        }
    }

    private static async Task SendConsoleMessagesAsync()
    {
        try
        {
            var writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            while (_client.Connected)
            {
                if (Console.KeyAvailable)
                {
                    var message = Console.ReadLine();
                    Console.WriteLine($"{message}");

                    if (message.ToLower() == "sair")
                    {
                        _client.Close();
                        break;
                    }
                    if (message.StartsWith("Arquivo"))
                    {
                        writer.WriteLine(message);
                    }
                    else if (message.StartsWith("Chat"))
                    {
                        await SendMessageToServerAsync(message);
                    }
                }

                await Task.Delay(100); // Pequena pausa para evitar loop de verificação intensivo
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
        }
    }

    private static async Task SendFileAsync(string fileName)
    {
        try
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                var fileInfo = new FileInfo(fileName);
                var hash = ComputeFileHash(fileStream);

                StreamWriter writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                // Enviar comando de arquivo
                await writer.WriteLineAsync($"Arquivo {fileInfo.Name}");
                await writer.WriteLineAsync($"Tamanho: {fileInfo.Length}");
                await writer.WriteLineAsync($"Hash: {hash}");

                // Enviar conteúdo do arquivo
                await fileStream.CopyToAsync(_stream);
                Console.WriteLine($"Arquivo {fileInfo.Name} enviado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar arquivo: {ex.Message}");
        }
    }

    private static async Task SendMessageToServerAsync(string message)
    {
        try
        {
            StreamWriter writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
            await writer.WriteLineAsync($"{message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem para o servidor: {ex.Message}");
        }
    }

    private static async Task ListenForServerMessagesAsync()
    {
        try
        {
            StreamReader reader = new StreamReader(_stream, Encoding.UTF8);

            while (_client.Connected)
            {
                var teste = _stream.Socket.Available;
                if (teste>0)
                {

                    var message = await reader.ReadLineAsync();

                    if (message != null)
                    {
                        if (message.StartsWith("Mensagem do servidor:"))
                        {
                            Console.WriteLine(message);
                        }
                        else if (message.StartsWith("Arquivo"))
                        {
                            var fileName = message.Substring(7).Trim();
                            await ReceiveFileAsync(fileName);
                        }
                    }
                }
                else
                {
                    await Task.Delay(100); // Pequena pausa para evitar loop de verificação intensivo
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na escuta de mensagens do servidor: {ex.Message}");
        }
    }

    private static async Task ReceiveFileAsync(string fileName)
    {
        try
        {
            StreamReader reader = new StreamReader(_stream, Encoding.UTF8);

            var status = await reader.ReadLineAsync();
            Console.WriteLine(status);

            if (status == "Status: nok")
            {
                status = await reader.ReadLineAsync();
                Console.WriteLine(status);
                return;
            }

            var name = (await reader.ReadLineAsync()).Substring(6);
            Console.WriteLine(name);

            var sizeString = (await reader.ReadLineAsync()).Substring(8);
            Console.WriteLine(sizeString);

            if (!long.TryParse(sizeString, out long size))
            {
                Console.WriteLine("Erro ao converter o tamanho do arquivo.");
                return;
            }

            var hash = (await reader.ReadLineAsync()).Substring(6);
            Console.WriteLine("Hash: " + hash);

            var buffer = new byte[size];
            using (var fileStream = new FileStream($"received_{fileName}", FileMode.Create, FileAccess.Write))
            {
                long totalBytesRead = 0;
                while (totalBytesRead < size)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao receber arquivo: {ex.Message}");
        }
    }

    private static string ComputeFileHash(FileStream fileStream)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
