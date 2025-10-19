using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskManagerClient
{
    public class CommandMessage
    {
        public int Command { get; set; }
        public JsonElement? Data { get; set; }
    }
}
