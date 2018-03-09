namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.EntityFrameworkCore
{
    public interface IDbCommand
    {
        string CommandText { get; }
    }
}
