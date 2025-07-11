namespace Demo
{
    public interface IDemo
    {
        Task Run(CancellationToken cancellationToken = default);
    }
}
