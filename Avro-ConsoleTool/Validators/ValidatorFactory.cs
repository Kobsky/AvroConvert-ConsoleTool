namespace Avro_ConsoleTool.Validators
{
    public class ValidatorFactory
    {
        public static IValidator Create(Commands command)
        {
            switch (command)
            {
                case Commands.ShemaToClass: 
                    return new ShemaToClassValidator();

                default:
                    return new HelpValidator();
            }
        }
    }
}
