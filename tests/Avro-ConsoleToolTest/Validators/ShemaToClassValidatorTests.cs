using Avro_ConsoleTool.Validators;

namespace Avro_ConsoleToolTest.Validators
{
    public class ShemaToClassValidatorTests
    {
        private const string ValidFirst = "stc";
        private const string ValidSecond = "my.avsc";
        private const string ValidThrid = "my.cs";

        [Fact]
        public void CheckTypeTest()
        {
            //Arrange
            var validator = new ShemaToClassValidator();

            //Assert
            Assert.Equal(Commands.ShemaToClass, validator.Type);
        }

        [Fact]
        public void CheckArgumentNumbersTest() 
        {
            //Arrange
            var validator = new ShemaToClassValidator();
            var args = new string[]
            {
                ValidFirst
            };

            //Act
            var result = validator.Validate(args);

            //Assert
            Assert.Equal(Messages.Validator_STC_InvalidArgumentsNumber, result.First());
        }

        [Fact]
        public void CheckShemaArgumentTest()
        {
            //Arrange
            var validator = new ShemaToClassValidator();
            var args = new string[]
            {
                ValidFirst,
                "invalid",
                ValidThrid
            };

            //Act
            var result = validator.Validate(args);

            //Assert
            Assert.Equal(Messages.Validator_STC_SecondParameterInvalid, result.First());
        }

        [Fact]
        public void CheckModelArgumentTest()
        {
            //Arrange
            var validator = new ShemaToClassValidator();
            var args = new string[]
            {
                ValidFirst,
                ValidSecond,
                "invalid"
            };

            //Act
            var result = validator.Validate(args);

            //Assert
            Assert.Equal(Messages.Validator_STC_ThridParameterInvalid, result.First());
        }
    }
}
