namespace Avro_ConsoleTool.Strategies
{
    public class HelpStrategy : IStrategy
    {
        public Commands Type => Commands.Help;

        public void Run(string[] args)
        {
            Console.WriteLine(Messages.HelpDescription);
        }
    }
}
