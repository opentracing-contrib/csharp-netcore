namespace Microsoft.Extensions.DependencyInjection
{
    public interface IOpenTracingBuilder
    {
        IServiceCollection Services { get; }
    }
}
