namespace OpenTracing.Contrib.NetCore.Interceptors.EntityFrameworkCore
{
    internal interface IDbCommand
    {
        string CommandText { get; }
    }
}
