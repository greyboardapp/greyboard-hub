namespace Greyboard.Core.Models;

public class BoardAccess
{
    public enum AccessType
    {
        Viewer,
        Editor,
        Admin,
    }

    public string Id { get; set; } = "";
    public string Board { get; set; } = "";
    public User? User { get; set; }

    public AccessType Type { get; set; }
}