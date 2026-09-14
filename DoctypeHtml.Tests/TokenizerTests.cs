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

    [Test]
    public void Script()
    {
        var content = File.ReadAllText("./TestData/Script.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }

    [Test]
    public void Styles()
    {
        var content = File.ReadAllText("./TestData/Styles.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }

    [Test]
    public void Meta()
    {
        var content = File.ReadAllText("./TestData/Meta.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }

    [Test]
    public void Comments()
    {
        var content = File.ReadAllText("./TestData/Comments.html").AsMemory();
        Tokenizer.Run(content, Console.WriteLine);
    }
}