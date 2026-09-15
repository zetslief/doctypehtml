using DoctypeHtml.Parser;

namespace DoctypeHtml.Tests;

public class TokenizerTests
{
    [Test]
    public void Basic()
    {
        var content = File.ReadAllText("./TestData/Basic.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Attributes()
    {
        var content = File.ReadAllText("./TestData/Attributes.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Popover()
    {
        var content = File.ReadAllText("./TestData/Popover.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Script()
    {
        var content = File.ReadAllText("./TestData/Script.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Styles()
    {
        var content = File.ReadAllText("./TestData/Styles.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Meta()
    {
        var content = File.ReadAllText("./TestData/Meta.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    [Test]
    public void Comments()
    {
        var content = File.ReadAllText("./TestData/Comments.html").AsMemory();
        Tokenizer.Run(content, TokenPrinter(Console.Write));
    }

    private static Action<Token> TokenPrinter(Action<string> write) => (token) =>
    {
        var message = token switch
        {
            DoctypeToken doctype => $"<!DOCTYPE {doctype.Name}>",
            StartTagToken start => start.SelfClosing ? $"<{start.Name} />" : $"<{start.Name}>",
            EndTagToken end => $"</{end.Name}>",
            CommentToken comment => $"<!--{comment.Data}-->",
            CharacterToken character => $"{character.Character}",
            EndOfFileToken _ => Environment.NewLine,
            var other => throw new NotImplementedException($"Token Printer is not implemented for {other}."),
        };
        write(message);
    };
}