using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetRabbitMQExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("please input rabbitmq hostname");

            // Get ホスト名
            var hostname = Console.ReadLine();

            // Taskキャンセルトークン
            var tokenSource = new CancellationTokenSource();

            Console.WriteLine($"start .Net RabbitMQ Example. Ctl+C to exit");
            
            // ファクトリ生成
            var factory = new ConnectionFactory()
            {
                HostName = hostname
            };

            // パブリッシャータスク
            var pTask = Task.Run(() => new Action<ConnectionFactory, CancellationToken>(async(f, cancel) => {
                // コネクション＆チャンネル生成
                using (var conn = f.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    // Exchange生成
                    channel.ExchangeDeclare("test", "fanout", false, true);

                    while(true)
                    {
                        // キャンセル待ち
                        if (cancel.IsCancellationRequested)
                        {
                            break;
                        }

                        var msg = new SendMessage()
                        {
                            Message = "Hello",
                            Timestamp = DateTime.UtcNow.ToBinary()
                        };
                        var body = JsonConvert.SerializeObject(msg);

                        // Publish!!
                        try
                        {
                            channel.BasicPublish("test", "", null, Encoding.UTF8.GetBytes(body));
                            Console.WriteLine($"success send. message: {msg.Message}, timestamp: {msg.Timestamp}");
                        } catch (Exception ex)
                        {
                            Console.WriteLine($"failer send. reason: {ex.Message}");
                        }

                        await Task.Delay(10000);
                    }
                }
            })(factory, tokenSource.Token), tokenSource.Token);

            // コンシューマータスク
            var cTask = Task.Run(() => new Action<ConnectionFactory, CancellationToken>((f, cancel) => {
                // コネクション＆チャンネル生成
                using (var conn = f.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    // Exchange生成
                    channel.ExchangeDeclare("test", "fanout", false, true);

                    // Queue生成
                    var queueName = channel.QueueDeclare().QueueName;

                    // Bind Queue
                    channel.QueueBind(queueName, "test", "");

                    // コンシューマー生成
                    var consumer = new EventingBasicConsumer(channel);

                    // 受信イベント定義
                    consumer.Received += (model, ea) =>
                    {
                        var msg = JsonConvert.DeserializeObject<ConsumedMessage>(Encoding.UTF8.GetString(ea.Body));
                        Console.WriteLine($"success consumed. message: {msg.Message}, timestamp: {msg.Timestamp}");
                    };

                    // コンシューマー登録
                    channel.BasicConsume(queueName, true, consumer);

                    while(true)
                    {
                        // キャンセル待ち   
                        if (cancel.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            })(factory, tokenSource.Token), tokenSource.Token);


            // Ctl+C待機
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                tokenSource.Cancel(); // Taskキャンセル
            };

            Task.WaitAll(pTask, cTask);

            Console.WriteLine("stop .Net RabbitMQ Example. press any key to close.");

            Console.ReadKey();
        }
    }
}
