using System.Text;
using DoctypeHtml.Parser;

namespace DoctypeHtml.Tests;

public class TokenizerTests
{
    [Test] public Task Basic() => Match("Basic.html");
    [Test] public Task Attributes() => Match("Attributes.html");
    [Test] public Task Comments() => Match("Comments.html");
    [Test] public Task Meta() => Match("Meta.html");
    [Test] public Task Popover() => Match("Popover.html");
    [Test] public Task Script() => Match("Script.html");
    [Test] public Task Styles() => Match("Styles.html");
    [Test] public Task Fragment() => Match("Fragment.html");

    private static async Task Match(string file)
    {
        var content = File.ReadAllText($"./TestData/{file}");
        var firstPass = new StringBuilder(content.Length);
        Tokenizer.Run(content.AsMemory(), TokenPrinter(m => firstPass.Append(m)));
        var firstResult = firstPass.ToString();
        var secondPass = new StringBuilder(firstPass.Length);
        Tokenizer.Run(firstResult.AsMemory(), TokenPrinter(m => secondPass.Append(m)));
        var secondResult = secondPass.ToString();
        Console.WriteLine(firstResult);
        await Assert.That(firstResult).IsEqualTo(secondResult);
    }

    private static Action<Token> TokenPrinter(Action<string> write) => (token) =>
    {
        static string AttributeToString((string Name, string Value) attribute)
            => attribute.Value.Length > 0 ? $"{attribute.Name}=\"{attribute.Value}\"" : $"{attribute.Name}";

        static string AttributesToString(IReadOnlyCollection<(string, string)> attributes)
            => attributes.Count == 0 ? string.Empty : ' ' + string.Join(' ', attributes.Select(AttributeToString));

        write(token switch
        {
            DoctypeToken doctype => $"<!DOCTYPE {doctype.Name}>",
            StartTagToken start => start.SelfClosing ? $"<{start.Name}{AttributesToString(start.Attributes)} />" : $"<{start.Name}{AttributesToString(start.Attributes)}>",
            EndTagToken end => $"</{end.Name}>",
            CommentToken comment => $"<!--{comment.Data}-->",
            CharacterToken character => $"{character.Character}",
            EndOfFileToken _ => Environment.NewLine,
            var other => throw new NotImplementedException($"Token Printer is not implemented for {other}."),
        });
    };
}