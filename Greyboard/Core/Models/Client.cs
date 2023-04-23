namespace Greyboard.Core.Models;

public class Client : User
{
    public string ConnectionId { get; set; } = "";
    public string Group { get; set; } = "";
    public float PointerX { get; set; } = 0;
    public float PointerY { get; set; } = 0;
    public PointerType PointerType { get; set; } = PointerType.Mouse;
    public bool Afk { get; set; } = false;
}