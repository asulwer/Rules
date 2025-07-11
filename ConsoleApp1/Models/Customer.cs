using System.ComponentModel.DataAnnotations;

namespace Demo.Models
{    
    public class Customer : IEquatable<Customer>
    {
        [Key]
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public bool Equals(Customer? c) => Id == c?.Id;
        public override bool Equals(object? obj) => Equals(obj as Customer);
        public override int GetHashCode() => Id.GetHashCode();        
    }
}
