using Avro_ConsoleTool.Strategies;
using Avro_ConsoleTool.Validators;

namespace Avro_ConsoleTool
{
    public class AvroConsole
    {
        public static void Main(string[] args)
        {
            var command = CommandParser.Parse(args);
            var validator = ValidatorFactory.Create(command);
            var valdationResult = validator.Validate(args);
            if(valdationResult.Any())
            {
                throw new ApplicationException(string.Join('\n',valdationResult));
            }
            var strategy = StrategyFactory.Create(command);
            strategy.Run(args);          
        }
    }
}