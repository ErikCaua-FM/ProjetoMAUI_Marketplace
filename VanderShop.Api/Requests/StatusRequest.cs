using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class StatusRequest
{
    [Required]
    public string Status { get; set; } = "";

    [Required]
    public string StatusAnterior { get; set; } = "";
}