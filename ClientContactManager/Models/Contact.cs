using System.ComponentModel.DataAnnotations;

namespace ClientContactManager.Models;

public class Contact
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public ICollection<ClientContact> ClientContacts { get; set; } = new List<ClientContact>();
}
