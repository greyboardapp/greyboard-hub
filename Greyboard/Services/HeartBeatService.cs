using Greyboard.Core.Managers;
using Greyboard.Hubs;
using Greyboard.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Greyboard.Services;

public class HeartBeatService : IHostedService
{
    private readonly IHubContext<BoardHub, IBoardClient> _hub;
    private readonly IBoardManager _boardManager;
    private readonly IClientManager _clientManager;
    private Timer? _timer;

    public HeartBeatService(IHubContext<BoardHub, IBoardClient> hub, IBoardManager boardManager, IClientManager clientManager)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        _boardManager = boardManager ?? throw new ArgumentNullException(nameof(boardManager));
        _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(async (object? state) => await HeartBeat(), null, 0, 100);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer != null)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        return Task.CompletedTask;
    }

    async Task HeartBeat()
    {
        foreach (var board in _boardManager.GetBoards())
        {
            var clients = _clientManager.GetClientsFromBoard(board.Slug);
            await _hub.Clients.All.HeartBeat(clients.ToList().ToDictionary(client => client.Id, client => new float[] { client.PointerX, client.PointerY, (float)client.PointerType }));
        }
    }
}