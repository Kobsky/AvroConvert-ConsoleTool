namespace Avro_ConsoleTool.Validators
{
    public class HelpValidator : IValidator
    {
        public Commands Type => Commands.Help;

        public IEnumerable<string> Validate(string[] args)
        {
            return new string[0];
        }
    }
}
