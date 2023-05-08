namespace Avro_ConsoleTool.Validators
{
    public interface IValidator
    {
        Commands Type { get; }

        IEnumerable<string> Validate(string[] args);
    }
}
