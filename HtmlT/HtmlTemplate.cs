using System.Text;
using System.Diagnostics;

using DoctypeHtml.Parser;

namespace HtmlT;

public record struct Result<T, E>(T? Value, E? Error) where E : struct
{
    public static Result<T, E> Ok(T value) => new(value, default);
    public static Result<T, E> Fail(E error) => new(default, error);

    public readonly bool IsError => Error.HasValue;
}

public static class ResultExtensions
{
    extension<T, E>(Result<T, E> result) where E: struct
    {
        public Result<T, E> ThenTry(Func<T, Result<T, E>> then)
        {
            if (result.IsError) return result;
            Debug.Assert(result.Value is not null);
            return then(result.Value);
        }

        public Result<NT, E> FailAs<NT>() => new(default, result.Error);
    }
}

public enum IOError
{
    FileNotFound
}

public abstract record Component;
public record HtmlComponent(string Content, Dictionary<string, Component> Children, Dictionary<string, string> DataSlots) : Component;
public record CssComponent(string Content) : Component();
public record JsComponent(string Content) : Component;

public record TextComponent(string Content) : Component; 
public record ListComponent(IEnumerable<HtmlComponent> Components) : Component;

public sealed class HtmlTemplate()
{
    public string Render(Component page)
    {
        var output = new StringBuilder(1000);
        switch (page)
        {
            case HtmlComponent html: Tokenizer.Run(html.Content.AsMemory(), t => output.Append(TokenToString(html, t))); break;
            case CssComponent css: output.Append($"<style>{css.Content}</style>"); break;
            case JsComponent js: output.Append($"<script>{js.Content}</script>"); break;
            case TextComponent text: output.Append(text.Content); break;
            case ListComponent list: foreach (var item in list.Components) output.Append(Render(item));
                break;
            default: throw new NotImplementedException($"{page.GetType()} is not supported. Component: {page}");
        }
        return output.ToString();
    }

    private string TokenToString(HtmlComponent html, Token token) => token switch
    {
        DoctypeToken doctype => $"<!DOCTYPE {doctype.Name}>",
        StartTagToken startTag => startTag.SelfClosing ? SelfClosingTagToString(html, startTag) : OpenTagToString(html, startTag),
        EndTagToken endTag => $"</{endTag.Name}>",
        CharacterToken character => $"{character.Character}",
        CommentToken comment => $"<!--{comment.Data}-->",
        EndOfFileToken _ => Environment.NewLine,
        var unknown => throw new NotImplementedException($"Token is not supported: {unknown}"),
    };

    private string SelfClosingTagToString(HtmlComponent component, StartTagToken token)
        => component.Children.TryGetValue(token.Name, out var found) ? Render(found) : OpenTagToString(component, token);

    private static string OpenTagToString(HtmlComponent component, StartTagToken token)
    {
        StringBuilder builder = new(100);
        builder.Append('<');
        builder.Append(token.Name);
        foreach (var (name, value) in token.Attributes)
        {
            if (component.DataSlots.TryGetValue(name, out var nameSlot))
            {
                if (string.IsNullOrEmpty(nameSlot)) continue; // removing attribute -> full skip.
                builder.Append(' ');
                builder.Append(nameSlot);
            }
            else
            {
                builder.Append(' ');
                builder.Append(name);
            }
            if (value is null) continue;
            builder.Append("='");
            builder.Append(component.DataSlots.TryGetValue(value, out var valueSlot) ? valueSlot : value);
            builder.Append('\'');
        }
        builder.Append(token.SelfClosing ? " />" : ">");
        return builder.ToString();
    }
}

public class ComponentBuilder(Component initialComponent)
{
    private readonly Component _component = initialComponent;

    public static HtmlComponentBuilder FromHtml(string content) => new(new HtmlComponent(content, [], []));
    public static Result<HtmlComponentBuilder, IOError> FromHtmlFile(string filePath) => File.Exists(filePath)
        ? Result<HtmlComponentBuilder, IOError>.Ok(FromHtml(File.ReadAllText(filePath)))
        : Result<HtmlComponentBuilder, IOError>.Fail(IOError.FileNotFound);

    public static ComponentBuilder FromCss(string content) => new(new CssComponent(content));
    public static Result<ComponentBuilder, IOError> FromCssFile(string filePath) => File.Exists(filePath)
        ? Result<ComponentBuilder, IOError>.Ok(FromCss(File.ReadAllText(filePath)))
        : Result<ComponentBuilder, IOError>.Fail(IOError.FileNotFound);

    public static ComponentBuilder FromJs(string content) => new(new JsComponent(content));
    public static Result<ComponentBuilder, IOError> FromJsFile(string filePath) => File.Exists(filePath)
        ? Result<ComponentBuilder, IOError>.Ok(FromJs(File.ReadAllText(filePath)))
        : Result<ComponentBuilder, IOError>.Fail(IOError.FileNotFound);
    
    public static TextComponent Text(string content) => new(content);
    public static ListComponent List(IEnumerable<HtmlComponent> components) => new(components);

    public Component Build() => _component;
}

public class HtmlComponentBuilder(HtmlComponent initialComponent)
{
    private readonly HtmlComponent _component = initialComponent;

    public HtmlComponentBuilder AddChild(string name, Component child) { _component.Children[name] = child; return this; }
    public HtmlComponentBuilder AddData(string slot, string value) { _component.DataSlots[slot] = value; return this; }

    public HtmlComponent Build() => _component;
}