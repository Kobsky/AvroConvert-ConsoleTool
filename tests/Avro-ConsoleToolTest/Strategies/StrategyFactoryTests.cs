using Avro_ConsoleTool.Strategies;

namespace Avro_ConsoleToolTest.Strategies
{
    public class StrategyFactoryTests
    {
        [Fact]
        public void HelpStrategyTest()
        {
            //Act
            var strategy = StrategyFactory.Create(Commands.Help);

            //Assert
            Assert.Equal(Commands.Help, strategy.Type);
        }

        [Fact]
        public void StcStrategyTest()
        {
            //Act
            var strategy = StrategyFactory.Create(Commands.ShemaToClass);

            //Assert
            Assert.Equal(Commands.ShemaToClass, strategy.Type);
        }
    }
}
