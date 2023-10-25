using Client;
using NLog;
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
            Server server = new Server(8001);
            Console.WriteLine("Сервер запущен. Для выхода нажмите Enter.");
            Console.ReadLine(); // Ждем, пока пользователь нажмет Enter, прежде чем завершить программу
        }
    }

    class Server
    {
        private int port;
        private UdpClient listener;

        public Server(int _port)
        {
            port = _port;
            listener = new UdpClient(_port);
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
                Console.Out.WriteLineAsync(ex.ToString());
            }
        }


        private void ProcessRequest(byte[] requestData, IPEndPoint clientEndPoint)
        {
            // Десериализация запроса
            string jsonRequest = Encoding.UTF8.GetString(requestData);
            Request request = Request.Deserialize(jsonRequest);

            // Обработка запроса (ваша логика здесь)
            // ...
            Console.Out.WriteLineAsync(request.Message);

            // Подготовка ответа
            Response response = new Response("Сервер получил ваш запрос и выполнил необходимые действия.", ResponseType.Success);
            string jsonResponse = response.Serialize();
            byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);

            // Отправка ответа асинхронно
            listener.SendAsync(responseData, responseData.Length, clientEndPoint);
        }
    }


    //public void SendMenu(IPEndPoint endPoint)
    //{
    //    byte[] data = ToByte("\x1b[3J\nМеню:\n1. Вывести все яхт-клубы\n2. Добавить новый яхт-клуб\n3. Удалить яхт-клуб по индексу\n4. Изменить яхт-клуб по индексу\n5. Загрузить яхт-клубы из файла\n6. Сохранить яхт-клубы в файл\n7. Выйти");
    //    listener.SendAsync(data,data.Length, endPoint);


    //}
    //    private async Task MenuChoose(string result, IPEndPoint endPoint)
    //    {
    //        await Console.Out.WriteLineAsync(result);
    //        switch (result)
    //        {
    //            case "1":
    //                {
    //                    await SendMessage(Encoding.Unicode.GetBytes("Вы выбрали 'Вывести все яхт-клубы'"), endPoint);
    //                    await DisplayYachtClubs(endPoint);
    //                    allDone.Set();
    //                }

    //                break;
    //            case "2":
    //                {
    //                    await SendMessage(Encoding.Unicode.GetBytes("Вы выбрали 'Добавить новый яхт-клуб'"), endPoint);
    //                    await AddYachtClub(endPoint);
    //                    allDone.Set();
    //                }

    //                break;
    //            case "3":
    //                //RemoveYachtClub(yachtClubController);
    //                break;
    //            case "4":
    //                //ModifyYachtClub(yachtClubController);
    //                break;
    //            case "5":
    //                yachtClubController.ReadAllRecords();
    //                allDone.Set();
    //                break;
    //            case "6":
    //                yachtClubController.WriteRecords();
    //                allDone.Set();
    //                break;
    //            case "7":
    //                Environment.Exit(0);
    //                break;
    //            default:
    //                await SendMessage(Encoding.Unicode.GetBytes("Неверный выбор"), endPoint);
    //                break;
    //        }
    //    }
    /*
    internal class Program
    {
        static void Main(string[] args)
        {
            NLog.LogManager.LoadConfiguration("D:/учёба 5 сем/архетектуры ИС/lab_2/NLog.config");
            Server server = new Server(8001);
            server.StartListenAsync();
            server.yachtClubController.WriteRecords();

        }

        
    }
    internal class Server
    {
        public YachtClubController yachtClubController;
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private static UdpClient? udpServer_S;

        
        public Server(int _port)
        {
            Logger.Info("Сервер робит");
            udpServer_S = new UdpClient(_port);
           
           
            yachtClubController = new YachtClubController();
           
           


            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Dispose();
            };
        }


        public async Task StartListenAsync()
        {
            while (true)
            {
                try
                {
                    allDone.Reset();
                    udpServer_S.BeginReceive(RequestCallBack, udpServer_S);
                    allDone.WaitOne();
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.ToString());
                }
            }

        }

        public async Task SendMessage(byte[] data, IPEndPoint endPoint)
        {
            await udpServer_S.SendAsync(data, data.Length, endPoint);
        }

        public async Task SendMenu(IPEndPoint endPoint)
        {
            allDone.Reset();
            string data = "\x1b[3J\nМеню:\n1. Вывести все яхт-клубы\n2. Добавить новый яхт-клуб\n3. Удалить яхт-клуб по индексу\n4. Изменить яхт-клуб по индексу\n5. Загрузить яхт-клубы из файла\n6. Сохранить яхт-клубы в файл\n7. Выйти";
            await SendMessage(Encoding.Unicode.GetBytes(data), endPoint);

            var result = Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer);
            await MenuChoose(result, endPoint);
            allDone.WaitOne();
        }


        private async void RequestCallBack(IAsyncResult ar)
        {
            allDone.Set();
            var listener = (UdpClient)ar.AsyncState;
            IPEndPoint endPoint = (IPEndPoint)udpServer_S.Client.LocalEndPoint;
            var res = listener.EndReceive(ar, ref endPoint);
            string result = Encoding.Unicode.GetString(res);
            Logger.Trace($"{endPoint.Address}:{endPoint.Port}:{result}");
            Console.WriteLine($"{endPoint.Address}:{endPoint.Port}:{result}");
            await SendMenu(endPoint);

        }

        private async Task MenuChoose(string result,IPEndPoint endPoint)
        {
            await Console.Out.WriteLineAsync(result);
            switch (result)
            {
                case "1":
                    {
                        await SendMessage(Encoding.Unicode.GetBytes("Вы выбрали 'Вывести все яхт-клубы'"), endPoint);
                        await DisplayYachtClubs(endPoint);
                        allDone.Set();
                    }
                    
                    break;
                case "2":
                    {
                        await SendMessage(Encoding.Unicode.GetBytes("Вы выбрали 'Добавить новый яхт-клуб'"), endPoint);
                        await AddYachtClub(endPoint);
                        allDone.Set();
                    }
                    
                    break;
                case "3":
                    //RemoveYachtClub(yachtClubController);
                    break;
                case "4":
                    //ModifyYachtClub(yachtClubController);
                    break;
                case "5":
                    yachtClubController.ReadAllRecords();
                    allDone.Set();
                    break;
                case "6":
                    yachtClubController.WriteRecords();
                    allDone.Set();
                    break;
                case "7":
                    Environment.Exit(0);
                    break;
                default:
                    await SendMessage(Encoding.Unicode.GetBytes("Неверный выбор"), endPoint);
                    break;
            }
        }

        private async Task AddYachtClub(IPEndPoint endPoint)
        {
            try
            {
                YachtClub obj = new YachtClub();

                await SendMessage(Encoding.Unicode.GetBytes("Введите название: "), endPoint);
                obj.Name = Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer);

                await SendMessage(Encoding.Unicode.GetBytes("Введите адрес: "), endPoint);
                obj.Address = Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer);

                await SendMessage(Encoding.Unicode.GetBytes("Введите количество яхт: "), endPoint);
                obj.NumberOfYachts = int.Parse(Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer));

                await SendMessage(Encoding.Unicode.GetBytes("Введите количество мест: "), endPoint);
                obj.NumberOfPlaces = int.Parse(Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer));

                await SendMessage(Encoding.Unicode.GetBytes("Есть ли бассейн(True/False): "), endPoint);
                obj.HasPool = bool.Parse(Encoding.Unicode.GetString(udpServer_S.ReceiveAsync().Result.Buffer));
                yachtClubController.AddRecord(obj);

            }
            catch (Exception ex) { 
                
            }
        }

        private async Task DisplayYachtClubs(IPEndPoint endPoint)
        {
            try
            {

                var list = yachtClubController.GetYachtClubs();
                for (int index = 0; index < list.Count; index++ )
                {
                    await SendMessage(Encoding.Unicode.GetBytes($"Индекс : {index}\nНазвание : {list[index].Name}\nАдрес : {list[index].Address}\nКоличество яхт : {list[index].NumberOfYachts}\nКоличество мест : {list[index].NumberOfPlaces}\nНаличие бассейна : {list[index].HasPool}"), endPoint);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        ~Server()
        {
            Dispose();
        }

        // Метод для явного закрытия ресурсов
        public void Dispose()
        {
            udpServer_S?.Close();
        }
    }
    */
}
