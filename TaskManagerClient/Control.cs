using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace TaskManagerClient
{
    public class ControlAsync
    {
        private Socket sock;
        private string ip;
        private int port;

        public static readonly object _lock = new object();

        public event Action<string> LogMessage;
        public event Action<string> ServerConnected;
        public event Action<string> ServerDisconnected;
        public ControlAsync(string ip = "127.0.0.1", int port = 49200)
        {
            this.ip = ip;
            this.port = port;
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task<List<string>> GetProcessesAsync()
        {
            return await Task.Run(() =>
            {
                List<string> list = new List<string>();
                try
                {
                    foreach (var proc in Process.GetProcesses())
                    {
                        lock (_lock)
                        {
                            list.Add(proc.ProcessName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return list;
            });
        }

        public async Task<string> SaveInFileAsync(string fileName, List<string> data)
        {
            //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            //string fullFileName = $"{fileName}_{timestamp}.json";
            string fullFileName = $"{fileName}.json";
            try
            {
                var option = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(data, option);

                await File.WriteAllTextAsync(fullFileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return fullFileName;
        }

        public async Task ConnectAsync()
        {

            try
            {
                IPAddress ipAddr = IPAddress.Parse(ip);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 49200);
                await sock.ConnectAsync(ipEndPoint);
                byte[] msg = Encoding.Default.GetBytes(Dns.GetHostName());
                await sock.SendAsync(msg, SocketFlags.None);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        public async Task SendFileAsync(string filePath)
        {
            try
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                byte[] lengthBytes = BitConverter.GetBytes(data.Length);

                // Сначала отправляем длину файла (4 байта)
                await sock.SendAsync(lengthBytes, SocketFlags.None);

                // Потом отправляем содержимое файла
                await sock.SendAsync(data, SocketFlags.None);

                Console.WriteLine($"Файл {Path.GetFileName(filePath)} отправлен на {ip}:{port}");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки файла: {ex.Message}");
            }
        }


        public void Close()
        {
            try
            {
                if (sock.Connected)
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    Console.WriteLine("Соединение закрыто.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии сокета: {ex.Message}");
            }
        }
    }
}
