using System.Threading.Tasks;
using Greyboard.Core;
using Greyboard.Core.Models;

namespace Greyboard.Hubs.Clients;

public interface IBoardClient
{
    Task Joined();
    Task BoardClosed();
    Task ReassignUserToClient();
    Task UserAllowedToSave(bool state);
    Task ConnectionReady(IEnumerable<Client> clients, IEnumerable<BoardEvent> actions, int age);
    Task ClientConnected(Client client);
    Task ClientDisconnected(Client client);
    Task ClientAfkUpdated(Client client);
    Task PerformBoardAction(BoardEvent action);
    Task BoardNameChanged(string name);
    Task BoardAged(int age);
    Task HeartBeat(Dictionary<string, float[]> pointers);
}
