using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Клиент2
{
    class Client
    {
        static void Main(string[] args)
        {
            try
            {
                TcpClient client = new TcpClient("localhost", 8888);
                Console.WriteLine("Connected to the server...");

                while (true)
                {
                    Console.Write("Enter action (1 - get a file, 2 - save a file, 3 - delete a file): > ");
                    string actionInput = Console.ReadLine();

                    if (actionInput == "exit")
                    {
                        SendRequest(client.GetStream(), "exit", "");
                        client.Close();
                        break;
                    }
                    else if (actionInput == "1" || actionInput == "2" || actionInput == "3")
                    {
                        string action = actionInput == "1" ? "GET" : actionInput == "2" ? "PUT" : "DELETE";

                        if (action == "PUT")
                        {
                            Console.Write("Enter name of the file: > ");
                            string fileName = Console.ReadLine();

                            Console.Write("Enter name of the file to be saved on server (Press Enter to use the same name): > ");
                            string serverFileName = Console.ReadLine();

                            if (string.IsNullOrWhiteSpace(serverFileName))
                                serverFileName = fileName;

                            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client", "data", fileName);

                            if (File.Exists(filePath))
                            {
                                long fileSize = new FileInfo(filePath).Length;

                                if (fileSize <= 1073741824)
                                {
                                    byte[] fileData = File.ReadAllBytes(filePath);
                                    string fileContent = Convert.ToBase64String(fileData);

                                    SendRequest(client.GetStream(), action, $"{serverFileName}={fileContent}");

                                    byte[] responseData = new byte[20073741];
                                    int bytesRead = client.GetStream().Read(responseData, 0, responseData.Length);
                                    string response = Encoding.ASCII.GetString(responseData, 0, bytesRead);

                                    if (response.StartsWith("200"))
                                    {
                                        Console.WriteLine("Response says that file is saved! ID = " + response.Substring(4));
                                    }
                                    else if (response == "403")
                                    {
                                        Console.WriteLine("Error: File already exists on the server.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Unexpected response: " + response);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Error: File size exceeds the limit of 1 gigabyte.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Error: File '{filePath}' not found.");
                            }
                        }
                        else if (action == "GET")
                        {
                            Console.Write("Do you want to get the file by name or by id (1 - name, 2 - id): > ");
                            string identifierTypeInput = Console.ReadLine();
                            string identifierType = identifierTypeInput == "1" ? "name" : identifierTypeInput == "2" ? "id" : "";

                            string fileIdOrName = "";
                            if (identifierType == "name")
                            {
                                Console.Write("Enter name: > ");
                                fileIdOrName = Console.ReadLine();
                            }
                            else if (identifierType == "id")
                            {
                                Console.Write("Enter id: > ");
                                fileIdOrName = Console.ReadLine();
                            }

                            SendRequest(client.GetStream(), action, $"{identifierType} {fileIdOrName}");

                            byte[] responseData = new byte[20073741];
                            int bytesRead = client.GetStream().Read(responseData, 0, responseData.Length);
                            string response = Encoding.ASCII.GetString(responseData, 0, bytesRead);

                            if (response.StartsWith("200"))
                            {
                                string[] parts = response.Split(' ');
                                if (parts.Length >= 3)
                                {
                                    string fileContent = parts[2];
                                    Console.Write("The file was downloaded! Specify a name for it: > ");
                                    string saveFileName = Console.ReadLine();
                                    byte[] fileData = Convert.FromBase64String(fileContent);
                                    string saveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client", "data", saveFileName);
                                    File.WriteAllBytes(saveFilePath, fileData);
                                    Console.WriteLine("File saved on the hard drive!");
                                }
                                else
                                {
                                    Console.WriteLine("Error: Unexpected response format.");
                                }
                            }
                            else if (response == "404")
                            {
                                Console.WriteLine("Error: File not found.");
                            }
                            else
                            {
                                Console.WriteLine("Unexpected response: " + response);
                            }
                        }
                        else if (action == "DELETE")
                        {
                            Console.Write("Do you want to delete the file by name or by id (1 - name, 2 - id): > ");
                            string identifierTypeInput = Console.ReadLine();
                            string identifierType = identifierTypeInput == "1" ? "name" : identifierTypeInput == "2" ? "id" : "";

                            string fileIdOrName = "";
                            if (identifierType == "name")
                            {
                                Console.Write("Enter name: > ");
                                fileIdOrName = Console.ReadLine();
                            }
                            else if (identifierType == "id")
                            {
                                Console.Write("Enter id: > ");
                                fileIdOrName = Console.ReadLine();
                            }

                            SendRequest(client.GetStream(), "DELETE", $"{identifierType} {fileIdOrName}");

                            byte[] responseData = new byte[1073741];
                            int bytesRead = client.GetStream().Read(responseData, 0, responseData.Length);
                            string response = Encoding.ASCII.GetString(responseData, 0, bytesRead);

                            if (response.StartsWith("200"))
                            {
                                Console.WriteLine("File successfully deleted.");
                            }
                            else if (response == "404")
                            {
                                Console.WriteLine("Error: File not found.");
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
}
