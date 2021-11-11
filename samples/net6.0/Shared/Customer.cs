namespace Shared
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }

        public Customer()
        {
        }

        public Customer(int customerId, string name)
        {
            CustomerId = customerId;
            Name = name;
        }
    }
}
