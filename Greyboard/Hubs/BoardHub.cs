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

    public async Task Join(User user, string slug)
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
                Id = user.Id,
                Name = user.Name,
                Avatar = user.Avatar,
                Group = slug,
            };

            await Groups.AddToGroupAsync(Context.ConnectionId, slug);
            _clientManager.AddClient(Context.ConnectionId, client);

            await Clients.Client(Context.ConnectionId).ConnectionReady(_clientManager.GetClientsFromBoard(slug), board.Actions);

            await Clients.GroupExcept(slug, Context.ConnectionId).ClientConnected(client);

            _logger.LogInformation($"User ({Context.ConnectionId}, {client.Id}) joined board ({slug})");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while joining client");
            Context.Abort();
        }
    }

    public void SetPointerPosition(float pointerX, float pointerY)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                client.PointerX = pointerX;
                client.PointerY = pointerY;
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

    public async Task AddItems(object data)
    {
        await AddBoardAction(BoardAction.ActionType.Add, data);
    }

    public async Task RemoveItems(object data)
    {
        await AddBoardAction(BoardAction.ActionType.Remove, data);
    }

    public async Task MoveItems(object data)
    {
        await AddBoardAction(BoardAction.ActionType.Move, data);
    }

    public async Task ResizeItems(object data)
    {
        await AddBoardAction(BoardAction.ActionType.Scale, data);
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
                    board.Actions.Clear();
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clear board actions");
        }
    }

    private async Task AddBoardAction(BoardAction.ActionType type, object data)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    var action = new BoardAction
                    {
                        By = client.Id,
                        Type = type,
                        Data = data
                    };
                    board.Actions.Add(action);
                    await Clients.Group(client.Group).BoardPerformAction(action);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add action");
        }
    }
}