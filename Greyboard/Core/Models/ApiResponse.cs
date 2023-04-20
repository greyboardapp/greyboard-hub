namespace Greyboard.Core.Models;

public class ApiResponse<T>
{
    public int Status { get; set; }
    public T? Result { get; set; }
    public string? Error { get; set; }
}