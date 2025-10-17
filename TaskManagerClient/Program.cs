namespace TaskManagerClient
{
    internal class Program
    {
        static async void Main(string[] args)
        {
            var client = new ControlAsync();
            try
            {
                await client.ConnectAsync();

                var processes = await client.GetProcessesAsync();
                string file = await client.SaveInFileAsync("processes", processes);
                await client.SendFileAsync(file);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
