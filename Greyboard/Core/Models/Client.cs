namespace Greyboard.Core.Models;

public class Client : User
{
    public string Group { get; set; } = "";
    public float PointerX { get; set; } = 0;
    public float PointerY { get; set; } = 0;
    public bool Afk { get; set; } = false;
}