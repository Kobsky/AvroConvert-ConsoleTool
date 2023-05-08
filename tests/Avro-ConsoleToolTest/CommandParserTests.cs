namespace Avro_ConsoleToolTest
{
    public class CommandParserTests
    {
        [Theory]
        [MemberData(nameof(HelpCases))]
        public void ParseHelpTest(object args)
        {
            Assert.Equal(Commands.Help, CommandParser.Parse((string[])args));
        }

        [Theory]
        [MemberData(nameof(ShemaToClassCases))]
        public void ParseShemaToClassTest(object args)
        {
            Assert.Equal(Commands.ShemaToClass, CommandParser.Parse((string[])args));
        }

        public static IEnumerable<object[]> HelpCases()
        {
            yield return new[] { new string[0] };
            yield return new[] { new string[1] { "-help" } };
            yield return new[] { new string[1] { "help" } };
            yield return new[] { new string[1] { "HeLp" } };
            yield return new[] { new string[1] { "Unknown" } };
        }

        public static IEnumerable<object[]> ShemaToClassCases()
        {
            yield return new[] { new string[1] { "-ShemaToClass" } };
            yield return new[] { new string[1] { "-stc" } };
            yield return new[] { new string[1] { "ShemaToclass" } };
            yield return new[] { new string[1] { "sTc" } };
        }

    }
}
