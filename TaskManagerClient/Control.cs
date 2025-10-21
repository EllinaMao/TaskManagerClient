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

        public event Action<string>? LogMessage;
        public event Action<string>? ServerConnected;
        public event Action<string>? ServerDisconnected;

        private SynchronizationContext? _uiContext = null;//winforms

        public ControlAsync(string ip = "127.0.0.1", int port = 49200, SynchronizationContext uiContext = null)
        {
            this.ip = ip;
            this.port = port;
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _uiContext = uiContext ?? new SynchronizationContext();

        }

        private void Log(string msg)
        {
            if (_uiContext != null)
                _uiContext.Post(d => LogMessage?.Invoke(msg), null);
            else
                LogMessage?.Invoke(msg);
        }

        public async Task<List<TaskManagerServer.ProcessInfo>> GetProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<TaskManagerServer.ProcessInfo>();
                try
                {
                    foreach (var proc in Process.GetProcesses())
                    {
                        lock (_lock)
                        {
                            list.Add(new TaskManagerServer.ProcessInfo
                            {
                                Id = proc.Id,
                                Name = proc.ProcessName
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error in getting processes: {ex.Message}");
                }
                return list;
            });
        }
        public async Task<string> SaveInFileAsync<T>(string fileName, List<T> data)
        {
            string fullFileName = $"{fileName}.json";
            try
            {
                string json = JsonSerializer.Serialize(data);
                json += "\n";
                await File.WriteAllTextAsync(fullFileName, json);
            }
            catch (Exception ex)
            {
                Log($"Saving file error: {ex.Message}");
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
                //byte[] msg = Encoding.Default.GetBytes(Dns.GetHostName());
                //await sock.SendAsync(msg, SocketFlags.None);
                //ServerConnected?.Invoke($"Connectes to {ip}:{port}");

            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.Message}");
            }

        }
        public async Task SendFileAsync(string filePath)
        {
            try
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                // Oтправляем содержимое файла
                await sock.SendAsync(data);

                Log($"File {Path.GetFileName(filePath)} sended at {ip}:{port}");
            }

            catch (Exception ex)
            {
                Log($"Send file error: {ex.Message}");
            }
        }

        public async Task ReceiveCommandsAsync()
        {
            byte[] buffer = new byte[1024];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (true)
                {
                    int bytesRead = await sock.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    sb.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
                    string json = sb.ToString();
                    if (TryParseCommand(json, out CommandMessage command))
                    {
                        await HandleCommandAsync(command);
                        sb.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Receive command error: {ex.Message}");
            }
            finally
            {
                ServerDisconnected?.Invoke("Server closed");
            }
        }

        private async Task HandleCommandAsync(CommandMessage command)
        {
            switch (command.Command)
            {
                case TaskManagerServer.ProcessCodes.GetProcesses:
                    var processes = await GetProcessesAsync();
                    string fileName = await SaveInFileAsync("Processes", processes);
                    await SendFileAsync(fileName);
                    break;
                case TaskManagerServer.ProcessCodes.KillProcess:
                    int id = command.Data?.GetProperty("Id").GetInt32() ?? -1;
                    if (id > 0)
                    {
                        try
                        {
                            Process.GetProcessById(id).Kill();
                            Log($"Process {id} stopped");
                        }
                        catch (Exception ex)
                        {
                            Log($"Error while stopped process {id}: {ex.Message}");
                        }
                    }
                    break;

                case TaskManagerServer.ProcessCodes.CreateProcess:
                    string path = command.Data?.GetProperty("Path").GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        try
                        {
                            Process.Start(path);
                            Log($"Process created: {path}");
                        }
                        catch (Exception ex)
                        {
                            Log($"Error creating process {path}: {ex.Message}");
                        }
                    }
                    break;

                default:
                    Log($"Unknown command: {command.Command}");
                    break;
            }
        }

        private bool TryParseCommand(string json, out CommandMessage? command)
        {
            command = null;
            try
            {
                command = JsonSerializer.Deserialize<CommandMessage>(json);
                return command != null;
            }
            catch (JsonException)
            {
                return false;
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
                    Console.WriteLine("Connection closed.");
                }
            }
            catch (Exception ex)
            {
                Log($"Socket Error: {ex.Message}");
            }
        }
    }
}
