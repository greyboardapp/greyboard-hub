using Greyboard.Core.Models;

namespace Greyboard.Core.Managers;

public interface IClientManager
{
    void AddClient(string id, Client client);
    void RemoveClient(string id);
    IEnumerable<Client> GetClientsFromBoard(string slug);
    bool AsClient(string id, out Client client);
}