using System.ComponentModel.DataAnnotations;

namespace Shared
{
    public class PlaceOrderCommand
    {
        [Required]
        public int? CustomerId { get; set; }

        [Required, StringLength(10)]
        public string ItemNumber { get; set; }

        [Required, Range(1, 100)]
        public int Quantity { get; set; }
    }
}
