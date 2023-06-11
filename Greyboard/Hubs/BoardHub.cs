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
    private readonly AppSettings _appSettings;
    private readonly IClientManager _clientManager;
    private readonly IBoardManager _boardManager;

    public BoardHub(ILogger<BoardHub> logger, AppSettings appSettings, IClientManager clientManager, IBoardManager boardManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
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

                    var clients = _clientManager.GetClientsFromBoard(client.Group);
                    if (!clients.Any())
                    {
                        _boardManager.RemoveBoard(client.Group);
                    }
                    else
                    {
                        if (board.Host == Context.ConnectionId)
                        {
                            FindNewBoardHost(board, clients);
                        }
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
                var httpContext = Context.Features.Get<IHttpContextFeature>()?.HttpContext;
                var origin = httpContext?.Request.Headers.Origin.ToString() ?? _appSettings.CLIENT_URLS.Split(";").FirstOrDefault("http://localhost:3000");
                var token = httpContext?.Request.Cookies["jwtToken"];
                board = await _boardManager.GetRemoteBoardData(origin, slug, token);
                if (board == null)
                {
                    throw new Exception("Board not valid");
                }

                board.Host = Context.ConnectionId;
                _boardManager.AddBoard(board);
                await Clients.Client(board.Host).UserAllowedToSave(true);
            }
            else
            {
                if (board.Author?.Id == user.Id || (string.IsNullOrEmpty(board.Host) && board.Accesses.Any(access => access.User?.Id == user.Id && access.Type >= BoardAccess.AccessType.Editor)))
                {
                    if (!string.IsNullOrEmpty(board.Host))
                    {
                        await Clients.Client(board.Host).UserAllowedToSave(false);
                    }
                    board.Host = Context.ConnectionId;
                    await Clients.Client(board.Host).UserAllowedToSave(true);
                }
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

            await Clients.Client(Context.ConnectionId).ConnectionReady(_clientManager.GetClientsFromBoard(slug), board.Events, board.Age);

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
                    if (board.isPublic || board.Author?.Id == client.Id || board.Accesses.Any(access => access.User?.Id == client.Id && access.Type >= BoardAccess.AccessType.Editor))
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
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add board action");
        }
    }

    public async void AccessesModified(IEnumerable<BoardAccess> accesses)
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    _logger.LogInformation($"Board accesses modified ({board.Slug})");
                    board.Accesses = accesses.ToList();
                    await Clients.GroupExcept(board.Slug, Context.ConnectionId).BoardAccessesModified(accesses);

                    if (_clientManager.AsClient(board.Host, out Client hostClient))
                    {
                        if (board.Accesses.Any(access => access.User?.Id == hostClient.Id && access.Type == BoardAccess.AccessType.Viewer))
                        {
                            FindNewBoardHost(board, _clientManager.GetClientsFromBoard(board.Slug));
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to notify board access modification");
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

    public async Task BoardSaved()
    {
        try
        {
            if (_clientManager.AsClient(Context.ConnectionId, out Client client))
            {
                var board = _boardManager.GetBoard(client.Group);
                if (board != null)
                {
                    board.Events.Clear();
                    board.Age++;
                    await Clients.Group(board.Slug).BoardAged(board.Age);
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

    private void FindNewBoardHost(Board board, IEnumerable<Client> clients)
    {
        Clients.Client(board.Host).UserAllowedToSave(false);
        try
        {
            var clientToBeHost = clients.First((client) => board.Accesses.Any(access => access.User?.Id == client.Id && access.Type >= BoardAccess.AccessType.Editor));
            board.Host = clientToBeHost.ConnectionId;
            Clients.Client(board.Host).UserAllowedToSave(true);
        }
        catch (InvalidOperationException)
        {
            board.Host = "";
        }
    }
}