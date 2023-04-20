using System.Threading.Tasks;
using Greyboard.Core;
using Greyboard.Core.Models;

namespace Greyboard.Hubs.Clients;

public interface IBoardClient
{
    Task Joined();
    Task ConnectionReady(IEnumerable<Client> clients, IEnumerable<BoardAction> actions);
    Task ClientConnected(Client client);
    Task ClientDisconnected(Client client);
    Task ClientAfkUpdated(Client client);
    Task BoardPerformAction(BoardAction action);
    Task HeartBeat(Dictionary<string, float[]> pointers);
}
