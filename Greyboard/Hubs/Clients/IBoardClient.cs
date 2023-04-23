using System.Threading.Tasks;
using Greyboard.Core;
using Greyboard.Core.Models;

namespace Greyboard.Hubs.Clients;

public interface IBoardClient
{
    Task Joined();
    Task BoardClosed();
    Task ReassignUserToClient();
    Task ConnectionReady(IEnumerable<Client> clients, IEnumerable<BoardEvent> actions);
    Task ClientConnected(Client client);
    Task ClientDisconnected(Client client);
    Task ClientAfkUpdated(Client client);
    Task PerformBoardAction(BoardEvent action);
    Task BoardNameChanged(string name);
    Task HeartBeat(Dictionary<string, float[]> pointers);
}
