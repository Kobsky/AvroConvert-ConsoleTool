namespace Avro_ConsoleTool.Strategies
{
    public class StrategyFactory
    {
        public static IStrategy Create(Commands command)
        {
            switch (command)
            {
                case Commands.ShemaToClass: 
                    return new ShemaToClassStrategy(); 
                default:
                    return new HelpStrategy();
            };
        }
    }
}
