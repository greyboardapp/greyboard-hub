namespace Greyboard.Core;

public class AppSettings
{
    public string CLIENT_URL { get; set; } = "http://localhost:8787";

    public AppSettings(IConfiguration configuration)
    {
        foreach (var property in this.GetType().GetProperties())
        {
            var value = configuration[property.Name];
            if (value != null)
            {
                property.SetValue(this, value);
            }
        }
    }
}