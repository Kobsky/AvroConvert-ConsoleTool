using Avro_ConsoleTool;
namespace Avro_ConsoleToolTest
{
    public class ConsoleToolTest
    {

        [Theory]
        [MemberData(nameof(HelpCases))]
        public void TestHelpMessage(object args)
        {
            //Arrange
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            //Act
            AvroConsole.Main((string[])args);

            //Assert
            Assert.Contains(Messages.HelpDescription, stringWriter.ToString());
        }

        public static IEnumerable<object[]> HelpCases()
        {
            yield return new[] { new string[0] };
            yield return new[] { new string[1] { "-help" } };
        }
    }
}