using System.Text.RegularExpressions;

namespace Avro_ConsoleTool.Validators
{
    public class ShemaToClassValidator : IValidator
    {
        private Regex _shema = new Regex("^.*[.]avsc$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private Regex _model = new Regex("^.*[.]cs$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public Commands Type => Commands.ShemaToClass;

        public IEnumerable<string> Validate(string[] args)
        {
            if (args.Length < 3)
                return new string[] { Messages.Validator_STC_InvalidArgumentsNumber };

            List<string> result = new List<string>();

            if (!_shema.IsMatch(args[1]))
                result.Add(Messages.Validator_STC_SecondParameterInvalid);

            if (!_model.IsMatch(args[2]))
                result.Add(Messages.Validator_STC_ThridParameterInvalid);

            return result;
        }
    }
}
