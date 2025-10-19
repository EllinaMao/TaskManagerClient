namespace TaskManagerClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var client = new ControlAsync();

            client.LogMessage += Console.WriteLine;
            client.ServerConnected += msg => Console.WriteLine(msg);
            client.ServerDisconnected += msg => Console.WriteLine(msg);

            Console.WriteLine("Подключение к серверу...");
            await client.ConnectAsync();

            // Начинаем слушать команды от сервера
            await client.ReceiveCommandsAsync();

            Console.WriteLine("Для выхода нажмите Enter...");
            Console.ReadLine();
            client.Close();
        }
    }
}
