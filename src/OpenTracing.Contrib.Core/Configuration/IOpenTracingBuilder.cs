namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An interface for configuring OpenTracing services.
    /// </summary>
    public interface IOpenTracingBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where OpenTracing services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
