using Greyboard.Core.Managers;
using Greyboard.Core.Models;

namespace Greyboard.Managers;

public class ClientManager : IClientManager
{
    private readonly ILogger<ClientManager> _logger;
    private readonly Dictionary<string, Client> _clients = new();

    public ClientManager(ILogger<ClientManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddClient(string id, Client client)
    {
        if (_clients.ContainsKey(id))
        {
            _clients[id] = client;
            _logger.LogInformation($"Assigning new client ({client.Id}) to connection ({id})");
        }
        else
        {
            _clients.Add(id, client);
            _logger.LogInformation($"Adding new client ({client.Id}) for connection ({id})");
        }
    }

    public void RemoveClient(string id)
    {
        if (_clients.Remove(id))
        {
            _logger.LogInformation($"Removing client ({id}) from connection ({id})");
        }
    }

    public bool AsClient(string id, out Client client)
    {
        return _clients.TryGetValue(id, out client);
    }

    public IEnumerable<Client> GetClientsFromBoard(string slug)
    {
        return _clients.Values.Where(client => client.Group == slug);
    }
}