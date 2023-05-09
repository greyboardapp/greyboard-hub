namespace Greyboard.Core.Models;

public class Board
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Author { get; set; } = "";

    public int Age { get; set; } = 0;
    public List<BoardEvent> Events { get; set; } = new();
    public string Host { get; set; } = "";
}