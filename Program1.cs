using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main(string[] args)
    {
        try
        {
            TcpClient client = new TcpClient("localhost", 8888);
            Console.WriteLine("Connected to the server...");

            Console.Write("Enter action (1 - get a file, 2 - create a file, 3 - delete a file): > ");
            string actionInput = Console.ReadLine();

            if (actionInput == "exit")
            {
                SendRequest(client.GetStream(), "exit", "");
                client.Close();
            }
            else if (actionInput == "1" || actionInput == "2" || actionInput == "3")
            {
                string action = actionInput == "1" ? "GET" : actionInput == "2" ? "PUT" : "DELETE";

                Console.Write("Enter filename: > ");
                string fileName = Console.ReadLine();
                string fileContent = "";

                if (action == "PUT")
                {
                    Console.Write("Enter file content: > ");
                    fileContent = Console.ReadLine();
                }

                SendRequest(client.GetStream(), action, $"{fileName}={fileContent}");

                // Обработка ответа сервера
                byte[] responseData = new byte[1024];
                int bytesRead = client.GetStream().Read(responseData, 0, responseData.Length);
                string response = Encoding.ASCII.GetString(responseData, 0, bytesRead);

                // Вывод сообщения в зависимости от действия и кода ответа сервера
                if (action == "GET")
                {
                    if (response.StartsWith("200"))
                    {
                        Console.WriteLine("The content of the file is: " + response.Substring(4));
                    }
                    else
                    {
                        Console.WriteLine("The response says: " + response);
                    }
                }
                else if (action == "PUT")
                {
                    if (response.StartsWith("200"))
                    {
                        Console.WriteLine("The response says that the file was created!");
                    }
                    else if (response.StartsWith("403"))
                    {
                        Console.WriteLine("The response says that creating the file was forbidden!");
                    }
                    else
                    {
                        Console.WriteLine("Unexpected response: " + response);
                    }
                }
                else if (action == "DELETE")
                {
                    if (response.StartsWith("200"))
                    {
                        Console.WriteLine("The response says that the file was successfully deleted!");
                    }
                    else if (response.StartsWith("404"))
                    {
                        Console.WriteLine("The response says that the file was not found!");
                    }
                    else
                    {
                        Console.WriteLine("Unexpected response: " + response);
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid action. Please enter 1, 2, 3, or exit.");
            }

            Console.WriteLine("Connection closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static void SendRequest(NetworkStream stream, string action, string data)
    {
        string request = $"{action} {data}";
        byte[] requestData = Encoding.ASCII.GetBytes(request);
        stream.Write(requestData, 0, requestData.Length);
        Console.WriteLine("The request was sent.");
    }
}
