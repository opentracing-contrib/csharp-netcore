namespace OpenTracing.Contrib.NetCore.Interceptors.EntityFrameworkCore
{
    public interface IDbCommand
    {
        string CommandText { get; }
    }
}
