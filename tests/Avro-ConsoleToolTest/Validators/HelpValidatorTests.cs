
using Avro_ConsoleTool.Validators;

namespace Avro_ConsoleToolTest.Validators
{
    public class HelpValidatorTests
    {
        [Fact]
        public void ValidInAnyCasesTest()
        {
            //Arrange
            var validator = new HelpValidator();

            //Act
            var result = validator.Validate(null);

            //Asser
            Assert.Empty(result);
        }
    }
}
