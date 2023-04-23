namespace Greyboard.Core.Models;

public class BoardAction
{
    public enum ActionType
    {
        None,
        Add,
        Remove,
        Move,
        Scale,
        Order,
        LockState,
    }

    public string By { get; set; } = "";
    public ActionType Type { get; set; }
    public object? Data { get; set; }
}