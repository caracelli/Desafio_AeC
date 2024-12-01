namespace Dominio.Interfaces
{
    public interface ILogService
    {
        void LogEvent(string tipo, string evento, string status, DateTime dataExecucao);
    }
}
