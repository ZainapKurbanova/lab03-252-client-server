using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Сервер2
{
    class Server
    {
        private static int fileIdCounter = 0;
        private static readonly object fileDictLock = new object();
        private static readonly Dictionary<int, string> fileIdToNameDict = new Dictionary<int, string>();

        static void Main(string[] args)
        {
            try
            {
                TcpListener server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                Console.WriteLine("Server started!");

                LoadFileIdDictionary();

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

                    // Создаем новый поток для обработки каждого подключения
                    Thread clientThread = new Thread(() => HandleClient(client, dataDirectory));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void HandleClient(TcpClient client, string dataDirectory)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                while (client.Connected)
                {
                    byte[] data = new byte[20073741];
                    int bytesRead = stream.Read(data, 0, data.Length);
                    string request = Encoding.ASCII.GetString(data, 0, bytesRead);

                    string[] parts = request.Split(' ');
                    string action = parts[0];

                    if (action == "exit")
                    {
                        string exitResponse = "200 Server stopped";
                        byte[] exitResponseData = Encoding.ASCII.GetBytes(exitResponse);
                        stream.Write(exitResponseData, 0, exitResponseData.Length);
                        Console.WriteLine("Response sent: " + exitResponse);
                        stream.Close();
                        client.Close();
                        Console.WriteLine("Client disconnected!");
                        break;
                    }

                    string response = "";

                    if (action == "PUT")
                    {
                        string fileName = parts[1].Split('=')[0];
                        string fileContent = parts.Length > 1 ? parts[1].Substring(fileName.Length + 1) : "";

                        string filePath = Path.Combine(dataDirectory, fileName);

                        try
                        {
                            if (File.Exists(filePath))
                            {
                                response = "403";
                                Console.WriteLine("Ошибка 403: Файл уже существует.");
                            }
                            else
                            {
                                byte[] fileData = Convert.FromBase64String(fileContent);
                                File.WriteAllBytes(filePath, fileData);
                                int fileId = GetNextFileId();
                                AddFileIdMapping(fileId, fileName);
                                response = "200 " + fileId.ToString();
                                Console.WriteLine("200: Файл сохранен. ID = " + fileId);
                            }
                        }
                        catch (Exception)
                        {
                            response = "500";
                            Console.WriteLine("Ошибка 500: Внутренняя ошибка сервера.");
                        }
                    }

                    else if (action == "GET")
                    {
                        string identifierType = parts[1];
                        string fileIdOrName = parts[2];

                        string filePath = "";

                        if (identifierType == "name")
                        {
                            filePath = Path.Combine(dataDirectory, fileIdOrName);
                        }
                        else if (identifierType == "id")
                        {
                            if (int.TryParse(fileIdOrName, out int fileId))
                            {
                                if (fileIdToNameDict.TryGetValue(fileId, out string fileName))
                                {
                                    filePath = Path.Combine(dataDirectory, fileName);
                                }
                                else
                                {
                                    response = "404";
                                    SendResponse(stream, response);
                                    Console.WriteLine("Ошибка 404: Файл не найден.");
                                    continue;
                                }
                            }
                            else
                            {
                                response = "400 Invalid file ID";
                                SendResponse(stream, response);
                                Console.WriteLine(" Ошибка 400: Неверный ID файла.");
                                continue;
                            }
                        }

                        if (File.Exists(filePath))
                        {
                            byte[] fileData = File.ReadAllBytes(filePath);
                            string fileContent = Convert.ToBase64String(fileData);
                            response = "200 " + fileIdOrName + " " + fileContent;
                            Console.WriteLine("200: Файл отправлен клиенту.");
                        }
                        else
                        {
                            response = "404";
                            Console.WriteLine("404: Файл не найден.");
                        }
                    }

                    else if (action == "DELETE")
                    {
                        string identifierType = parts[1];
                        string fileIdOrName = parts[2];

                        string filePath = "";

                        if (identifierType == "name")
                        {
                            filePath = Path.Combine(dataDirectory, fileIdOrName);
                        }
                        else if (identifierType == "id")
                        {
                            if (int.TryParse(fileIdOrName, out int fileId))
                            {
                                if (fileIdToNameDict.TryGetValue(fileId, out string fileName))
                                {
                                    filePath = Path.Combine(dataDirectory, fileName);
                                }
                                else
                                {
                                    response = "404";
                                    SendResponse(stream, response);
                                    Console.WriteLine("Ошибка 404: Файл не найден.");
                                    continue;
                                }
                            }
                            else
                            {
                                response = "400 Invalid file ID";
                                SendResponse(stream, response);
                                Console.WriteLine("Ошибка 400: Неверный ID файла.");
                                continue;
                            }
                        }

                        if (File.Exists(filePath))
                        {
                            try
                            {
                                File.Delete(filePath);
                                response = "200";
                                Console.WriteLine("200: Файл удален.");
                            }
                            catch (Exception)
                            {
                                response = "500";
                                Console.WriteLine("Ошибка 500: Внутренняя ошибка сервера при удалении файла.");
                            }
                        }
                        else
                        {
                            response = "404";
                            Console.WriteLine("404: Файл не найден.");
                        }
                    }

                    SendResponse(stream, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static int GetNextFileId()
        {
            lock (fileDictLock)
            {
                return ++fileIdCounter;
            }
        }

        static void AddFileIdMapping(int fileId, string fileName)
        {
            lock (fileDictLock)
            {
                fileIdToNameDict[fileId] = fileName;
                SaveFileIdDictionary();
            }
        }

        static void LoadFileIdDictionary()
        {
            string fileIdDictFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server", "fileIdDictionary.txt");
            if (File.Exists(fileIdDictFilePath))
            {
                string[] lines = File.ReadAllLines(fileIdDictFilePath);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int fileId))
                    {
                        fileIdToNameDict[fileId] = parts[1];
                        if (fileId > fileIdCounter)
                        {
                            fileIdCounter = fileId;
                        }
                    }
                }
            }
        }

        static void SaveFileIdDictionary()
        {
            string fileIdDictFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server", "fileIdDictionary.txt");
            using (StreamWriter writer = new StreamWriter(fileIdDictFilePath))
            {
                foreach (KeyValuePair<int, string> entry in fileIdToNameDict)
                {
                    writer.WriteLine(entry.Key + "," + entry.Value);
                }
            }
        }

        static void SendResponse(NetworkStream stream, string response)
        {
            byte[] responseData = Encoding.ASCII.GetBytes(response);
            stream.Write(responseData, 0, responseData.Length);
            Console.WriteLine("Response sent: " + response);
        }
    }
}
