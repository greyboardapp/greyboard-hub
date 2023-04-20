namespace Greyboard.Core.Models;

public class Board
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public List<BoardAction> Actions { get; set; } = new();
}