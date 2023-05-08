
namespace Avro_ConsoleTool
{
    public class CommandParser
    {
        private const string Help = "Help";
        private const string ShemaToClass = "ShemaToClass";
        private const string Stc = "STC";

        public static Commands Parse(string[] args)
        {
            if (args is null || args.Length == 0)
                return Commands.Help;
            var command = args[0];

            if(command.Contains(Help, StringComparison.InvariantCultureIgnoreCase))
                return Commands.Help;

            if(command.Contains(ShemaToClass, StringComparison.InvariantCultureIgnoreCase))
                return Commands.ShemaToClass;

            if (command.Contains(Stc, StringComparison.InvariantCultureIgnoreCase))
                return Commands.ShemaToClass;
            
            return Commands.Help;
        }
    }
}
