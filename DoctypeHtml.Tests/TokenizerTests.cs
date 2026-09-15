using System.Text;
using DoctypeHtml.Parser;

namespace DoctypeHtml.Tests;

public class TokenizerTests
{
    [Test]
    [Arguments("Basic.html")]
    [Arguments("Attributes.html")]
    [Arguments("Comments.html")]
    [Arguments("Meta.html")]
    [Arguments("Popover.html")]
    [Arguments("Script.html")]
    [Arguments("Styles.html")]
    public async Task Match(string file)
    {
        var content = File.ReadAllText($"./TestData/{file}");
        var output = new StringBuilder(content.Length);
        Tokenizer.Run(content.AsMemory(), TokenPrinter(m => output.Append(m)));
        var result = output.ToString();
        await Assert.That(content).IsEqualTo(result);
    }

    private static Action<Token> TokenPrinter(Action<string> write) => (token) =>
    {
        static string AttributeToString((string Name, string Value) attribute)
            => $"{attribute.Name}=\"{attribute.Value}\"";
        static string AttributesToString(IEnumerable<(string, string)> attributes)
            => ' ' + string.Join(' ', attributes.Select(AttributeToString));

        var message = token switch
        {
            DoctypeToken doctype => $"<!DOCTYPE {doctype.Name}>",
            StartTagToken start => start.SelfClosing ? $"<{start.Name}{AttributesToString(start.Attributes)}/>" : $"<{start.Name}{AttributesToString(start.Attributes)}>",
            EndTagToken end => $"</{end.Name}>",
            CommentToken comment => $"<!--{comment.Data}-->",
            CharacterToken character => $"{character.Character}",
            EndOfFileToken _ => Environment.NewLine,
            var other => throw new NotImplementedException($"Token Printer is not implemented for {other}."),
        };
        write(message);
    };
}