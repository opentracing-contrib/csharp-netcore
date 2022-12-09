using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DataStore;

public class Order
{
    [Key]
    public int OrderId { get; set; }
        
    public int CustomerId { get; set; }

    [Required, StringLength(10)]
    public string ItemNumber { get; set; } = string.Empty;

    [Required, Range(1, 100)]
    public int Quantity { get; set; }
}
