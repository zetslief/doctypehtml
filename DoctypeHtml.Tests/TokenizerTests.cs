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

    [Test]
    public void Attributes()
    {
        var content = File.ReadAllText("./TestData/Attributes.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }

    [Test]
    public void Popover()
    {
        var content = File.ReadAllText("./TestData/Popover.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }
}