namespace ClientContactManager.Models;

public class ClientContact
{
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
}
