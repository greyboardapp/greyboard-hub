using System.Threading.Tasks;
using Greyboard.Core;
using Greyboard.Core.Managers;
using Greyboard.Core.Models;
using Greyboard.Hubs.Clients;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;

namespace Greyboard.Hubs;

public class BoardHub : Hub<IBoardClient>
{
    private readonly ILogger<BoardHub> _logger;
    private readonly IClientManager _clientManager;
    private readonly IBoardManager _boardManager;

    public BoardHub(ILogger<BoardHub> logger, IClientManager clientManager, IBoardManager boardManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
        _boardManager = boardManager ?? throw new ArgumentNullException(nameof(boardManager));
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation($"New connection ({Context.ConnectionId})");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    Clients.Group(client.Group).ClientDisconnected(client);

                    _clientManager.RemoveClient(Context.ConnectionId);

                    if (!_clientManager.GetClientsFromBoard(client.Group).Any())
                    {
                        _boardManager.RemoveBoard(client.Group);
                    }
                }
            }
            _logger.LogInformation($"Connection lost ({Context.ConnectionId}, {exception})");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to handle connection loss.");
        }
        return base.OnDisconnectedAsync(exception);
    }

    public async Task Join(Client user, string slug)
    {
        try
        {
            var board = _boardManager.GetBoard(slug);

            if (board == null)
            {
                var token = Context.Features.Get<IHttpContextFeature>()?.HttpContext?.Request.Cookies["jwtToken"];
                board = await _boardManager.GetRemoteBoardData(slug, token);
                if (board == null)
                {
                    throw new Exception("Board not valid");
                }

                _boardManager.AddBoard(board);
            }

            var client = new Client
            {
                ConnectionId = Context.ConnectionId,
                Id = user.Id,
                Name = user.Name,
                Avatar = user.Avatar,
                Group = slug,
                PointerX = user.PointerX,
                PointerY = user.PointerY
            };

            var boardClients = _clientManager.GetClientsFromBoard(slug);
            var clientsWithSameUser = boardClients.Where((client) => client.Id == user.Id);
            foreach (var c in clientsWithSameUser)
            {
                await Clients.Client(c.ConnectionId).ReassignUserToClient();
                await Clients.Group(client.Group).ClientDisconnected(c);
                _clientManager.RemoveClient(c.ConnectionId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, slug);
            _clientManager.AddClient(Context.ConnectionId, client);

            await Clients.Client(Context.ConnectionId).ConnectionReady(_clientManager.GetClientsFromBoard(slug), board.Events);

            await Clients.GroupExcept(slug, Context.ConnectionId).ClientConnected(client);

            _logger.LogInformation($"User ({Context.ConnectionId}, {client.Id}) joined board ({slug})");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while joining client");
            Context.Abort();
        }
    }

    public void SetPointerPosition(float pointerX, float pointerY, PointerType type)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                client.PointerX = pointerX;
                client.PointerY = pointerY;
                client.PointerType = type;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set pointer position");
        }
    }

    public async Task SetAfk(bool afk)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                client.Afk = afk;
                await Clients.Group(client.Group).ClientAfkUpdated(client);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set afk");
        }
    }

    public async Task BoardActionPerformed(object action)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    var boardEvent = new BoardEvent
                    {
                        By = client.Id,
                        Action = action
                    };
                    board.Events.Add(boardEvent);
                    await Clients.Group(client.Group).PerformBoardAction(boardEvent);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add board action");
        }
    }

    public async void CloseBoard()
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    _logger.LogInformation($"Board closed ({board.Slug})");
                    await Clients.GroupExcept(board.Slug, Context.ConnectionId).BoardClosed();
                    _boardManager.RemoveBoard(board.Slug);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clear board actions");
        }
    }

    public void BoardSaved()
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    board.Events.Clear();
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clear board actions");
        }
    }

    public async void SetBoardName(string name)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    board.Name = name;
                    await Clients.Group(client.Group).BoardNameChanged(name);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clear board actions");
        }
    }
}