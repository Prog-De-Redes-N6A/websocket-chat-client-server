using System.Net.WebSockets;
using System.Text;

namespace ConsoleClientWS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new SimpleWsClient("ws://localhost:5041/ws");
            await client.ConnectAsync();

            // Start receiving in background
            _ = Task.Run(client.ReceiveLoopAsync);

            // Send loop on main thread
            while (client.IsOpen)
            {
                string message = Console.ReadLine()!;
                await client.SendAsync(message);

                if (message.ToLower() == "exit")
                    break;
            }

            await client.CloseAsync();
        }
    }

    class SimpleWsClient
    {
        private readonly string _uri;
        private readonly ClientWebSocket _ws = new();

        public bool IsOpen => _ws.State == WebSocketState.Open;

        public SimpleWsClient(string uri)
        {
            _uri = uri;
        }

        public async Task ConnectAsync()
        {
            Console.WriteLine($"Connecting to {_uri}...");
            await _ws.ConnectAsync(new Uri(_uri), CancellationToken.None);
            Console.WriteLine("Connected!");
        }

        public async Task SendAsync(string message)
        {
            if (!IsOpen) return;

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            //Console.WriteLine($"[YOU] {message}");
        }

        public async Task ReceiveLoopAsync()
        {
            var buffer = new byte[4096];

            while (IsOpen)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive error: {ex.Message}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server closed the connection.");
                    await CloseAsync();
                    break;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[SERVER] {msg}");
            }
        }

        public async Task CloseAsync()
        {
            if (IsOpen)
            {
                await _ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client closing",
                    CancellationToken.None);
            }

            _ws.Dispose();
        }
    }
}
