using System.ComponentModel.DataAnnotations;

namespace ClientContactManager.Models;

public class ContactFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surname is required.")]
    [MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [MaxLength(150)]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    public string Email { get; set; } = string.Empty;

    public List<ClientSummaryViewModel> LinkedClients { get; set; } = new();
    public List<ClientSummaryViewModel> AvailableClients { get; set; } = new();
}
