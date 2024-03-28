using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main(string[] args)
    {
        try
        {
            TcpListener server = new TcpListener(IPAddress.Any, 8888);
            server.Start();
            Console.WriteLine("Server started!");

            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server", "data");
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
                Console.WriteLine("Data directory created: " + dataDirectory);
            }

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected!");

                NetworkStream stream = client.GetStream();

                byte[] data = new byte[1024];
                int bytesRead = stream.Read(data, 0, data.Length);
                string request = Encoding.ASCII.GetString(data, 0, bytesRead);
                Console.WriteLine("Request received: " + request);

                string[] parts = request.Split(' ');
                string action = parts[0];

                if (action == "exit")
                {
                    string exitResponse = "200 Server stopped";
                    byte[] exitResponseData = Encoding.ASCII.GetBytes(exitResponse);
                    stream.Write(exitResponseData, 0, exitResponseData.Length);
                    Console.WriteLine("Response sent: " + exitResponse);
                    server.Stop();
                    Console.WriteLine("Server stopped!");
                    break;
                }

                string fileName = parts[1].Split('=')[0];
                string fileContent = parts.Length > 1 ? parts[1].Substring(fileName.Length + 1) : "";

                string response = "";

                string filePath = Path.Combine(dataDirectory, fileName);

                if (action == "PUT")
                {
                    if (File.Exists(filePath))
                    {
                        response = "403 Forbidden";
                    }
                    else
                    {
                        try
                        {
                            File.WriteAllText(filePath, fileContent);
                            response = "200 File created";
                        }
                        catch (Exception)
                        {
                            response = "500 Internal Server Error";
                        }
                    }
                }
                else if (action == "GET")
                {
                    if (File.Exists(filePath))
                    {
                        fileContent = File.ReadAllText(filePath);
                        response = $"200 {fileContent}";
                    }
                    else
                    {
                        response = "404 File not found";
                    }
                }
                else if (action == "DELETE")
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        response = "200 File deleted";
                    }
                    else
                    {
                        response = "404 File not found";
                    }
                }
                else
                {
                    response = "400 Bad Request";
                }

                byte[] responseData = Encoding.ASCII.GetBytes(response);
                stream.Write(responseData, 0, responseData.Length);
                Console.WriteLine("Response sent: " + response);

                // Отключение клиента
                stream.Close();
                client.Close();
                Console.WriteLine("Client disconnected!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
