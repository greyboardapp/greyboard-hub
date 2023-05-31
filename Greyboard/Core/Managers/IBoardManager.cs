using Greyboard.Core.Models;

namespace Greyboard.Core.Managers;

public interface IBoardManager
{
    void AddBoard(Board board);
    void RemoveBoard(string slug);
    IEnumerable<Board> GetBoards();
    Board? GetBoard(string slug);
    Task<Board> GetRemoteBoardData(string origin, string slug, string? token);
}