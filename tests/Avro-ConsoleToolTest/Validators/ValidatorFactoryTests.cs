using Avro_ConsoleTool.Validators;

namespace Avro_ConsoleToolTest.Validators
{
    public class ValidatorFactoryTests
    {
        [Fact]
        public void HelpValidatorTest()
        {
            //Act
            var validator = ValidatorFactory.Create(Commands.Help);

            //Assert
            Assert.Equal(Commands.Help, validator.Type);
        }

        [Fact]
        public void UnknownValidatorTest()
        {
            //Act
            var validator = ValidatorFactory.Create((Commands)int.MaxValue);

            //Assert
            Assert.Equal(Commands.Help, validator.Type);
        }

        [Fact]
        public void ShemaToClassValidatorTest()
        {
            //Act
            var validator = ValidatorFactory.Create(Commands.ShemaToClass);

            //Assert
            Assert.Equal(Commands.ShemaToClass, validator.Type);
        }

    }
}
