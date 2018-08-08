using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApp3
{
    public class UdpPlayer
    {
        UdpClient client;
        Task task;
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        public void Open(int port)
        {
            client = new UdpClient(port);
            client.Client.ReceiveBufferSize = 1024 * 1024 * 1024;
            task = Task.Run(ListenMessage, tokenSource.Token);
        }

        public async Task ListenMessage()
        {
            while (true)
            {
                var result = await client.ReceiveAsync();
                tokenSource.Token.ThrowIfCancellationRequested();
                this.OnRecieve(result.Buffer);
            }
        }

        public int data_index = 0;
        public byte[] recieve_data = new byte[1024 * 1024 * 1024];

        private void OnRecieve(byte[] data)
        {
            Array.Copy(data, 0, recieve_data, data_index, data.Length);
            data_index += data.Length;
        }

        private static readonly int SEND_LENGTH = 65507; // 65507, 1410

        private byte[] send_buffer = new byte[SEND_LENGTH];

        public void Send(byte[] data, int port)
        {
            for(int i = 0; ; ++i)
            {
                if(data.Length - i * SEND_LENGTH < SEND_LENGTH)
                {
                    Array.Copy(data, i * SEND_LENGTH, send_buffer, 0, data.Length - i * SEND_LENGTH);
                    client.Send(send_buffer, data.Length - i * SEND_LENGTH, new IPEndPoint(IPAddress.Loopback, port));
                    break;
                }
                else
                {
                    Array.Copy(data, i * SEND_LENGTH, send_buffer, 0, send_buffer.Length);
                    client.Send(send_buffer, send_buffer.Length, new IPEndPoint(IPAddress.Loopback, port));
                }
            }
        }

        public void Close()
        {
            client.Close();
            tokenSource.Cancel();
        }
    }


    class Test : IDisposable
    {
        public void Dispose()
        {
            int a = 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var logger = log4net.LogManager.GetLogger("Program");

            UdpPlayer player = new UdpPlayer();
            player.Open(60128);

            UdpPlayer sender = new UdpPlayer();
            sender.Open(0);

            var rander = new Random();

            byte[] vs = new byte[200000000];
            for (int i = 0; i < vs.Length; ++i)
            {
                vs[i] = (byte)rander.Next();
            }

            var sw = new System.Diagnostics.Stopwatch();

            sw.Start();

            sender.Send(vs, 60128);


            bool check = false;
            for (; ; )
            {
                if (!check && player.data_index == vs.Length)
                {
                    sw.Stop();

                    bool success = true;

                    logger.Info($"millisec={sw.ElapsedMilliseconds}");

                    for (int i = 0; i < vs.Length; ++i)
                    {
                        if (vs[i] != player.recieve_data[i])
                        {
                            success = false;
                        }
                    }
                    check = true;

                    logger.Info(success);
                }

                Thread.Sleep(100);
            }
        }
    }
}
