using System;
using System.IO;
using System.Net;
using System.Net.Mime;
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

        var breakedRequest = request.Split(" ");

        if (breakedRequest[0] == "GET")
        {
            var breakedFileLocation = breakedRequest[1].Split("/");
            var fileName = breakedFileLocation[breakedFileLocation.Length-1];
            var breakedFileName = fileName.Split(".");

            var fileExtension = breakedFileName[breakedFileName.Length-1];

            switch (fileExtension)
            {
                case "html":
                {
                    HandleHTMLPage("pages"+breakedRequest[1], client);
                    break;
                }
                case "jpeg":
                {
                    HandleImages(breakedRequest[1], fileExtension,client);
                    break;
                }
                case "jpg":
                {
                    HandleImages(breakedRequest[1].Substring(1), fileExtension, client);
                    break;
                }
                case "png":
                {
                    HandleImages(breakedRequest[1].Substring(1), fileExtension, client);
                    break;
                }
                default:
                {

                    break; 
                }
                
            }
        }

        client.Close();
    }

    private static void HandleImages(string fileLocation, string fileExtension, TcpClient client)
    {
        string contentType = GetContentType(fileExtension);

        var response = "";

        if (!File.Exists(fileLocation))
        {
            response = NotFoundReponse();
            SendResponse(client, response);

            Console.WriteLine("Arquivo inexistente.");
            return;
        }

        SendResponse(client, response);

        SendFile(fileLocation, contentType, client);

    }

    private static string NotFoundReponse()
    {
        return "HTTP/1.0 404 Not Found\r\n" +
                    "Content-Type: text/html; charset=UTF-8\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "\r\n" +
                    "<html><body><h1>404 Not Found</h1></body></html>";
    }

    private static void HandleHTMLPage(string fileLocation, TcpClient client)
    {
        var response = "";

        if (!File.Exists(fileLocation))
        {
            response = NotFoundReponse();

            Console.WriteLine("Arquivo inexistente.");
            SendResponse(client, response);

            return;
        }

        string htmlContent = File.ReadAllText(fileLocation);

        var fileInfo = new FileInfo(fileLocation);


        response = "HTTP/1.0 200 Ok\r\n" +
          "Content-Type: text/html; charset=ASCII\r\n" +
          "Access-Control-Allow-Origin: *\r\n" +
          "\r\n";

        response += htmlContent;

        SendResponse(client, response);

    }

    public static void SendFile(string fileLocation, string contentType, TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            FileInfo fileInfo = new FileInfo(fileLocation);
            long fileSize = fileInfo.Length;

            // Build and send the HTTP header
            string response =
                "HTTP/1.0 200 OK\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {fileSize}\r\n" +
                "\r\n";

            writer.Write(Encoding.ASCII.GetBytes(response));

            // Send the file content
            using (FileStream fileStream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
    }
    private static void SendResponse(TcpClient client, string response)
    {
        NetworkStream stream = client.GetStream();
        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
        stream.Write(responseBytes, 0, responseBytes.Length);
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
    private static string GetContentType(string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            case "jpg":
            case "jpeg":
                return "image/jpeg";
            case "png":
                return "image/png";
            case "gif":
                return "image/gif";
            default:
                return "application/octet-stream";
        }
    }
}