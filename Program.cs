namespace BrodcastChatServer;

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
class Program
{
    private static TcpListener _server;
    private static List<TcpClient> _clients = new List<TcpClient>();
    private static object _lock = new object();

    static async Task Main(string[] args)
    {
        _server = new TcpListener(IPAddress.Any, 5000);
        _server.Start();
        Console.WriteLine("Server started on port 5000...");

        while (true)
        {
            var client = await _server.AcceptTcpClientAsync();
            lock (_lock)
            {
                _clients.Add(client);

            }
            await BroadcastMessageAsync("A new user joined the chat.", client);

            _ = HandleClientAsync(client);

        }
    }
    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {message}");

                // Send message to all clients
                await BroadcastMessageAsync(message, client);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Client disconnected unexpectedly.");
        }
        finally
        {
            lock (_lock) _clients.Remove(client);
            client.Close();
            await BroadcastMessageAsync("A user left the chat.");
        }
    }
    private static async Task BroadcastMessageAsync(string message, TcpClient? excludeClient = null)
    {

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");

        lock (_lock)
        {
            foreach (TcpClient client in _clients)
            {
                if (client == excludeClient)
                {
                    continue;
                }
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.WriteAsync(data, 0, data.Length);
                }
                catch
                {
                    // Ignore broken connections
                }
            }
        }
    }
}
