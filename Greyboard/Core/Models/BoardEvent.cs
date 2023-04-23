namespace Greyboard.Core.Models;

public class BoardEvent
{
    public string By { get; set; } = "";
    public object? Action { get; set; }
}