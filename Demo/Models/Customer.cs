using System.ComponentModel.DataAnnotations;

namespace Demo.Models
{
    /// <summary>
    /// Sample customer model used in rule expressions and actions.
    /// </summary>
    public class Customer : IEquatable<Customer>
    {
        /// <summary>
        /// Unique identifier for the customer.
        /// </summary>
        [Key]
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Customer full name. Can be modified by rule actions.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the customer account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Compares customers by Id.
        /// </summary>
        /// <param name="c">Other customer to compare.</param>
        /// <returns>True if same Id.</returns>
        public bool Equals(Customer? c) => Id == c?.Id;

        /// <summary>
        /// Compares customers by Id.
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as Customer);

        /// <summary>
        /// Hash code based on Id.
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();        
    }
}
