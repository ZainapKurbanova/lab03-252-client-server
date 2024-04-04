using System; 
using System.IO;  
using System.Net; 
using System.Net.Sockets;  
using System.Text;  

namespace Сервер1  
{
    class Server  
    {
        static void Main(string[] args)  
        {
            try  
            {
                // Создание TcpListener, прослушивающего все IP-адреса на порту 8888
                TcpListener server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                Console.WriteLine("Server started!");  // Вывод сообщения о запуске сервера

                // Проверка наличия и создание директории для хранения данных, если она отсутствует
                string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server", "data");
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                    Console.WriteLine("Data directory created: " + dataDirectory);  // Вывод сообщения о создании директории
                }

                while (true)  // Бесконечный цикл для принятия клиентских подключений
                {
                    // Принятие клиентского подключения
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected!");  // Вывод сообщения о подключении клиента

                    // Получение потока для обмена данными с клиентом
                    NetworkStream stream = client.GetStream();

                    // Чтение запроса от клиента
                    byte[] data = new byte[1024];
                    int bytesRead = stream.Read(data, 0, data.Length);
                    string request = Encoding.ASCII.GetString(data, 0, bytesRead);
                    Console.WriteLine("Request received: " + request);  // Вывод полученного запроса

                    // Разбиение запроса на части для получения действия и данных
                    string[] parts = request.Split(' ');
                    string action = parts[0];

                    // Обработка запроса в зависимости от действия
                    if (action == "exit")  // Если действие - выход
                    {
                        // Отправка ответа и остановка сервера при получении команды выхода
                        string exitResponse = "200 Server stopped";
                        byte[] exitResponseData = Encoding.ASCII.GetBytes(exitResponse);
                        stream.Write(exitResponseData, 0, exitResponseData.Length);
                        Console.WriteLine("Response sent: " + exitResponse);  // Вывод отправленного ответа
                        server.Stop();  // Остановка сервера
                        Console.WriteLine("Server stopped!");  // Вывод сообщения остановки сервера
                        break;  // Выход из цикла
                    }

                    // Извлечение имени файла и его содержимого из запроса
                    string fileName = parts[1].Split('=')[0];
                    string fileContent = parts.Length > 1 ? parts[1].Substring(fileName.Length + 1) : "";

                    // Формирование пути к файлу
                    string filePath = Path.Combine(dataDirectory, fileName);

                    // Формирование ответа в зависимости от действия
                    string response = "";

                    if (action == "PUT")  // Если действие - добавить файл
                    {
                        if (File.Exists(filePath))  // Если файл уже существует
                        {
                            response = "403 Forbidden";  // Ответ с кодом 403 (Запрещено)
                        }
                        else  // Если файл не существует
                        {
                            try
                            {
                                File.WriteAllText(filePath, fileContent);  // Создание файла и запись содержимого
                                response = "200 File created";  // Ответ с кодом 200 (Файл создан)
                            }
                            catch (Exception)
                            {
                                response = "500 Internal Server Error";  // Ответ с кодом 500 (Внутренняя ошибка сервера)
                            }
                        }
                    }
                    else if (action == "GET")  // Если действие - получить файл
                    {
                        if (File.Exists(filePath))  // Если файл существует
                        {
                            fileContent = File.ReadAllText(filePath);  // Чтение содержимого файла
                            response = $"200 {fileContent}";  // Ответ с кодом 200 (Успешно) и содержимым файла
                        }
                        else  // Если файл не существует
                        {
                            response = "404 File not found";  // Ответ с кодом 404 (Файл не найден)
                        }
                    }
                    else if (action == "DELETE")  // Если действие - удалить файл
                    {
                        if (File.Exists(filePath))  // Если файл существует
                        {
                            File.Delete(filePath);  // Удаление файла
                            response = "200 File deleted";  // Ответ с кодом 200 (Файл удален)
                        }
                        else  // Если файл не существует
                        {
                            response = "404 File not found";  // Ответ с кодом 404 (Файл не найден)
                        }
                    }
                    else  // Если действие не распознано
                    {
                        response = "400 Bad Request";  // Ответ с кодом 400 (Неверный запрос)
                    }

                    // Отправка ответа клиенту
                    byte[] responseData = Encoding.ASCII.GetBytes(response);  // Кодирование ответа в массив байт
                    stream.Write(responseData, 0, responseData.Length);  // Отправка массива байт в поток сокета
                    Console.WriteLine("Response sent: " + response);  // Вывод отправленного ответа

                    // Отключение клиента
                    stream.Close();  // Закрытие потока
                    client.Close();  // Закрытие клиента
                    Console.WriteLine("Client disconnected!");  // Вывод сообщения об отключении клиента
                }
            }
            catch (Exception ex)  // Обработка исключений
            {
                Console.WriteLine("Error: " + ex.Message);  // Вывод сообщения об ошибке
            }
        }
    }
}
