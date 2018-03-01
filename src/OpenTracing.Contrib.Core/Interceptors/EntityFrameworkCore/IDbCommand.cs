namespace OpenTracing.Contrib.Core.Interceptors.EntityFrameworkCore
{
    internal interface IDbCommand
    {
        string CommandText { get; }
    }
}
