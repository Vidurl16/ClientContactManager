namespace ClientContactManager.Models;

public class ClientIndexItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public int ContactCount { get; set; }
}
