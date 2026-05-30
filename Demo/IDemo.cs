namespace Demo
{
    /// <summary>
    /// Interface for runnable demos. Each implementation is auto-discovered and executed by Program.
    /// </summary>
    public interface IDemo
    {
        /// <summary>
        /// Executes the demo logic.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async cooperation.</param>
        /// <returns>Task representing the async operation.</returns>
        Task Run(CancellationToken cancellationToken = default);
    }
}
