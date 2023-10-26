using Client;
using NLog;
using NLog.Config;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;


namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");

            Server server = new Server(8001);
            Console.WriteLine("Сервер запущен. Для выхода нажмите Enter.");
            Console.ReadLine(); // Ждем, пока пользователь нажмет Enter, прежде чем завершить программу

    }
}

    class Server
    {
        private YachtClubController yachtClubController;
        private int port;
        private UdpClient listener;
        private readonly Logger Logger;

        public Server(int _port)
        {
            Logger = LogManager.GetCurrentClassLogger();

            port = _port;
            listener = new UdpClient(_port);
            yachtClubController = new YachtClubController();
            Logger.Info("Сервер запущен.");

            yachtClubController.ReadAllRecords();
            Task.Run(() => StartListenAsync());
        }

        private async Task StartListenAsync()
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await listener.ReceiveAsync();

                    if (result.Buffer != null && result.Buffer.Length > 0)
                    {
                        ProcessRequest(result.Buffer, result.RemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        private void ProcessRequest(byte[] requestData, IPEndPoint clientEndPoint)
        {
            StringBuilder message = new StringBuilder(" Запрос получен"); ;
            ResponseType responseType = ResponseType.Success;
            // Десериализация запроса
            string jsonRequest = Encoding.UTF8.GetString(requestData);
            Request request = Request.Deserialize(jsonRequest);

            // Обработка запроса (ваша логика здесь)
            // ...
            Console.Out.WriteLineAsync($"{request.MessageType}:{request.Message}");
            switch (request.MessageType)
            {
                case RequestType.Delete:
                    try
                    {
                        if (request.Parametrs.TryGetValue("Index", out string indexString) && int.TryParse(indexString, out int index))
                        {
                            yachtClubController.RemoveRecord(index);
                            message = new StringBuilder("Запись успешно удалена.");
                            responseType = ResponseType.Success;
                            yachtClubController.WriteRecords();
                        }
                        else
                        {
                            message = new StringBuilder("Ошибка: Некорректный формат индекса.");
                            responseType = ResponseType.Error;
                        }
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        Logger.Error(ex);

                        message = new StringBuilder($"Ошибка: Некорректный индекс");
                        responseType = ResponseType.Error;
                    }
                    break;
                case RequestType.GetAll:
                    message = new StringBuilder();
                    var list = yachtClubController.GetYachtClubs();
                    for (int index = 0; index < list.Count; index++)
                    {
                        message.Append($"Индекс : {index}" +
                            $"\nНазвание : {list[index].Name}" +
                            $"\nАдрес : {list[index].Address}" +
                            $"\nКоличество яхт : {list[index].NumberOfYachts}" +
                            $"\nКоличество мест : {list[index].NumberOfPlaces}" +
                            $"\nНаличие бассейна : {list[index].HasPool}" +
                            $"\n----------------------------------\n");
                    }

                    break;
                case RequestType.GetOne:
                    try
                    {
                        if (request.Parametrs.TryGetValue("Index", out string indexString) && int.TryParse(indexString, out int index))
                        {
                            var el = yachtClubController.GetYachtClub(index);
                            message = new StringBuilder($"Индекс : {index}" +
                            $"\nНазвание : {el.Name}" +
                            $"\nАдрес : {el.Address}" +
                            $"\nКоличество яхт : {el.NumberOfYachts}" +
                            $"\nКоличество мест : {el.NumberOfPlaces}" +
                            $"\nНаличие бассейна : {el.HasPool}" +
                            $"\n----------------------------------");
                            responseType = ResponseType.Success;
                            yachtClubController.WriteRecords();
                        }
                        else
                        {
                            message = new StringBuilder("Ошибка: Некорректный формат индекса.");
                            responseType = ResponseType.Error;
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        Logger.Error(ex);

                        message = new StringBuilder($"Ошибка: Некорректный индекс");
                        responseType = ResponseType.Error;
                    }
                    break;
                case RequestType.Post:
                    try
                    {
                        string name, address;
                        int numberOfYachts, numberOfPlaces;
                        bool hasPool;

                        if (request.Parametrs.TryGetValue("Name", out name) &&
                            request.Parametrs.TryGetValue("Address", out address) &&
                            request.Parametrs.TryGetValue("NumberOfYachts", out string numberOfYachtsStr) &&
                            int.TryParse(numberOfYachtsStr, out numberOfYachts) &&
                            request.Parametrs.TryGetValue("NumberOfPlaces", out string numberOfPlacesStr) &&
                            int.TryParse(numberOfPlacesStr, out numberOfPlaces) &&
                            request.Parametrs.TryGetValue("HasPool", out string hasPoolStr) &&
                            bool.TryParse(hasPoolStr, out hasPool))
                        {
                            YachtClub newYachtClub = new YachtClub
                            {
                                Name = name,
                                Address = address,
                                NumberOfYachts = numberOfYachts,
                                NumberOfPlaces = numberOfPlaces,
                                HasPool = hasPool
                            };
                            yachtClubController.AddRecord(newYachtClub);
                            message = new StringBuilder("Запись успешно добавлена.");
                            responseType = ResponseType.Success;
                            yachtClubController.WriteRecords();
                        }
                        else
                        {
                            message = new StringBuilder("Ошибка: Некорректные параметры запроса.");
                            responseType = ResponseType.Error;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);

                        message = new StringBuilder($"Ошибка: {ex.Message}");
                        responseType = ResponseType.Error;
                    }
                    break;

                case RequestType.Menu:
                    message = new StringBuilder("Список команд:\n- delete\n- getAll\n- getOne\n- post\n- exit");
                    break;
                case RequestType.Uncorrect:
                    responseType = ResponseType.Error;
                    message = new StringBuilder("неверная команда");
                    break;


            }

            ServerResponse(message,responseType,clientEndPoint);
        }

        private void ServerResponse(StringBuilder message,ResponseType respType,IPEndPoint clientEndPoint)
        {
            // Подготовка ответа
            Response response = new Response(message.ToString(), respType);
            string jsonResponse = response.Serialize();
            byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);

            // Отправка ответа асинхронно
            listener.SendAsync(responseData, responseData.Length, clientEndPoint);
        }
    }
}
