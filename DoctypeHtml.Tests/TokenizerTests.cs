using DoctypeHtml.Parser;

namespace DoctypeHtml.Tests;

public class TokenizerTests
{
    [Test]
    public void Basic()
    {
        var content = File.ReadAllText("./TestData/Basic.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }
}
