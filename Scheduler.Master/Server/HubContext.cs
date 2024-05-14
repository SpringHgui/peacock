using MySqlX.XDevAPI;

namespace Scheduler.Master.Server
{
    public class HubContext
    {
        public IEnumerable<Client> Clients { get; set; }

        public Client GetClient(string id)
        {
            return null;
        }
    }

    public class Client
    {
        public Task SendAsync<T>(string name, T molde)
        {
            return Task.CompletedTask;
        }
    }
}
