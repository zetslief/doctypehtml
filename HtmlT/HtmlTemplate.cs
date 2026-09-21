using System.Text;

using DoctypeHtml.Parser;

namespace HtmlT;

public record Component(string Content, Dictionary<string, Component> Children);


public sealed class HtmlTemplate()
{
    public string Render(Component page)
    {
        var output = new StringBuilder(page.Content.Length * 2);
        Tokenizer.Run(page.Content.AsMemory(), t => output.Append(TokenToString(page, t)));
        return output.ToString();
    }

    private string TokenToString(Component component, Token token) => token switch
    {
        DoctypeToken doctype => $"<!DOCTYPE {doctype.Name}>",
        StartTagToken startTag => startTag.SelfClosing ? SelfClosingTagToString(component, startTag) : $"<{startTag.Name}>",
        EndTagToken endTag => $"</{endTag.Name}>",
        CharacterToken character => $"{character.Character}",
        CommentToken comment => $"<!--{comment.Data}-->",
        EndOfFileToken _ => Environment.NewLine,
        var unknown => throw new NotImplementedException($"Token is not supported: {unknown}"),
    };

    private string SelfClosingTagToString(Component component, StartTagToken token)
        => component.Children.TryGetValue(token.Name, out var found) ? Render(found) : $"<{token.Name} />";
}
