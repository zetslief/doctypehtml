using System.Text;

using DoctypeHtml.Parser;

namespace HtmlT;

public record struct Result<T, E>(T? Value, E? Error) where E : struct
{
    public static Result<T, E> Ok(T value) => new(value, default);
    public static Result<T, E> Fail(E error) => new(default, error);

    public readonly bool IsError => Error.HasValue;
}

public enum IOError
{
    FileNotFound
}

public abstract record Component(string Content, Dictionary<string, Component> Children);
public record HtmlComponent(string Content, Dictionary<string, Component> Children) : Component(Content, Children);
public record CssComponent(string Content, Dictionary<string, Component> Children) : Component($"<style>{Content}</style>", Children);
public record JsComponent(string Content, Dictionary<string, Component> Children) : Component($"<script>{Content}</script>", Children);

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
    {
        return component.Children.TryGetValue(token.Name, out var found)
            ? Render(found)
            : $"<{token.Name} {string.Join(' ', token.Attributes.Select(a => $"{a.Name}='{a.Value}'"))}/>";
    }
}

public class ComponentBuilder(Component initialComponent)
{
    private readonly Component _component = initialComponent;

    public static ComponentBuilder FromHtml(string content) => new(new HtmlComponent(content, []));
    public static Result<ComponentBuilder, IOError> FromHtmlFile(string filePath) => File.Exists(filePath)
        ? Result<ComponentBuilder, IOError>.Ok(FromHtml(File.ReadAllText(filePath)))
        : Result<ComponentBuilder, IOError>.Fail(IOError.FileNotFound);

    public static ComponentBuilder FromCss(string content) => new(new CssComponent(content, []));
    public static Result<ComponentBuilder, IOError> FromCssFile(string filePath) => File.Exists(filePath)
        ? Result<ComponentBuilder, IOError>.Ok(FromCss(File.ReadAllText(filePath)))
        : Result<ComponentBuilder, IOError>.Fail(IOError.FileNotFound);

    public static ComponentBuilder FromJs(string content) => new(new JsComponent(content, []));
    public static Result<ComponentBuilder, IOError> FromJsFile(string filePath) => File.Exists(filePath)
        ? Result<ComponentBuilder, IOError>.Ok(FromJs(File.ReadAllText(filePath)))
        : Result<ComponentBuilder, IOError>.Fail(IOError.FileNotFound);

    public ComponentBuilder AddChild(string name, Component script) { _component.Children[name] = script; return this; }

    public Component Build() => _component;
}