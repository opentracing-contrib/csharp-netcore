using System.ComponentModel.DataAnnotations;

namespace Shared;

public class PlaceOrderCommand
{
    [Required, Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required, StringLength(10)]
    public string ItemNumber { get; set; } = string.Empty;

    [Required, Range(1, 100)]
    public int Quantity { get; set; }
}
