namespace Avro_ConsoleTool.Strategies
{
    public interface IStrategy
    {
        Commands Type { get; }

        void Run(string[] args);
    }
}
