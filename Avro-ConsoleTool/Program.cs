namespace Avro_ConsoleTool
{
    public class AvroConsole
    {
        private const string HelpOption = "-help";

        public static void Main(string[] args)
        {
            if(args == null 
                || args.Length == 0 
                || (args.Length > 0 && args[0].Equals(HelpOption, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.WriteLine(Messages.HelpDescription);
            }
        }
    }
}