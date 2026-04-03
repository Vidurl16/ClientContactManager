using System.ComponentModel.DataAnnotations;

namespace ClientContactManager.Models;

public class ClientFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string ClientCode { get; set; } = string.Empty;

    public List<ContactSummaryViewModel> LinkedContacts { get; set; } = new();
    public List<ContactSummaryViewModel> AvailableContacts { get; set; } = new();
}
