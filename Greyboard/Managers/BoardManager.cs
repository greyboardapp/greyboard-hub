using Greyboard.Core;
using Greyboard.Core.Managers;
using Greyboard.Core.Models;

namespace Greyboard.Managers;

public class BoardManager : IBoardManager
{
    private readonly ILogger<BoardManager> _logger;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, Board> _boards = new();

    public BoardManager(ILogger<BoardManager> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public void AddBoard(Board board)
    {
        if (_boards.TryAdd(board.Slug, board))
        {
            _logger.LogInformation($"Board ({board.Slug}) created");
        }
    }

    public void RemoveBoard(string slug)
    {
        if (_boards.Remove(slug))
        {
            _logger.LogInformation($"Board ({slug}) removed");
        }
    }

    public IEnumerable<Board> GetBoards()
    {
        return _boards.Values;
    }

    public Board? GetBoard(string slug)
    {
        if (_boards.TryGetValue(slug, out Board? board))
        {
            return board;
        }
        return null;
    }

    public async Task<Board> GetRemoteBoardData(string origin, string slug, string? token)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add("Cookie", $"jwtToken={token}");
            }

            var url = $"{origin}/api/boards/slug/{slug}";

            _logger.LogInformation($"Requesting board data from {url}");

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<ApiResponse<Board>>();
            if (data == null || data.Status != 200 || data.Result == null)
            {
                throw new HttpRequestException(data?.Error ?? "unknown");
            }

            return data.Result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to get board data from remote");
            return null;
        }
    }
}