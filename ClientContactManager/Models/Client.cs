using System.ComponentModel.DataAnnotations;

namespace ClientContactManager.Models;

public class Client
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    public string ClientCode { get; init; } = string.Empty;

    public ICollection<ClientContact> ClientContacts { get; set; } = new List<ClientContact>();
}
