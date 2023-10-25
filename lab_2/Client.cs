﻿using Server;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();

        }
    }
    class Client
    {
        private IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001);
        private UdpClient client;

        public Client()
        {
            client = new UdpClient();
            client.Connect(serverEP);
            StartAsync().Wait(); // Дождитесь завершения StartAsync перед завершением конструктора
        }

        private async Task StartAsync()
        {
            Console.WriteLine("Введите сообщение (или 'exit' для выхода):");

            while (true)
            {
                string message = await Console.In.ReadLineAsync();

                if (message.ToLower() == "exit")
                {
                    break;
                }

                await SendRequestAsync(message);
            }

            client.Close();
        }

        private async Task SendRequestAsync(string message)
        {
            Request request = new Request(message, RequestType.Post);
            string jsonRequest = request.Serialize();
            byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
            await client.SendAsync(requestData, requestData.Length);
            Console.WriteLine("Сообщение отправлено на сервер.");
            await ReceiveResponseAsync();
        }

        private async Task ReceiveResponseAsync()
        {
            UdpReceiveResult result = await client.ReceiveAsync();
            string jsonResponse = Encoding.UTF8.GetString(result.Buffer);
            Response response = Response.Deserialize(jsonResponse);
            Console.WriteLine($"Сервер прислал: {response.Message}");
        }
    }

    /*
    internal class Client
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001);
        private IPEndPoint localEndPoint;
        private TcpListener listener;
        public static UdpClient udpClient;


        public Client()
        {

            Logger.Info("client ready");
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            localEndPoint = (IPEndPoint)listener.LocalEndpoint;
            listener.Stop(); 
            
            udpClient = new UdpClient(localEndPoint.Port);

            Autorize();      

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Dispose();
            };
        }

        private async Task Autorize()
        {
            await SendMessageToServer(Encoding.Unicode.GetBytes($"Клиент с портом {localEndPoint.Port} подключен"));
            StartListenAsync();
        }

        public void StartListenAsync()
        {
            while (true)
            {
                allDone.Reset();
                udpClient.BeginReceive(RequestCallback, udpClient);
                allDone.WaitOne();
            }
        }
        public async Task SendMessageToServer(byte[] data)
        {
            
            try
            {
                await udpClient.SendAsync(data, data.Length, serverEndPoint);
            }
            catch (Exception ex)
            {
                // Обработка ошибки отправки данных
                Console.WriteLine($"Ошибка отправки данных: {ex.Message}");
            }

            
        }
        


        private void RequestCallback(IAsyncResult ar)
        {
            
            try
            {
                allDone.Set();
                var listener = (UdpClient)ar.AsyncState;
                var res = listener.EndReceive(ar, ref serverEndPoint);
                string data = Encoding.Unicode.GetString(res);
                Console.WriteLine( data);
                
                string key = Console.ReadLine();
                SendMessageToServer(Encoding.Unicode.GetBytes(key));
                

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            
            
            
        }
        ~Client()
        {
            Dispose();
        }

        // Метод для явного закрытия ресурсов
        public void Dispose()
        {
            udpClient?.Close();
            listener?.Stop();
        }


    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Client client;
            NLog.LogManager.LoadConfiguration("D:/учёба 5 сем/архетектуры ИС/lab_2/NLog.config");

            try
            {
                client = new Client();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания клиента: {ex.Message}");
                return;
            }

            
        }
    }

    */
};