using System.Text;

namespace DoctypeHtml.Parser;

public interface IBuilder
{
    Token Build();
}

public interface IBuilder<T> : IBuilder where T : Token
{
    new T Build();
    Token IBuilder.Build() => Build();
}

public abstract record Token()
{
    internal static EndOfFileToken CreateEndOfFileToken() => new();
}

public record DoctypeToken(string Name) : Token
{
    public sealed class Builder : IBuilder<DoctypeToken>
    {
        private StringBuilder NameBuilder => field ??= new();
        public Builder AppendToName(char @char) { NameBuilder.Append(@char); return this; }
        public DoctypeToken Build() => new(NameBuilder.ToString());
    }
}

public record StartTagToken(string Name) : Token
{
    public sealed class Builder : IBuilder<StartTagToken>
    {
        private StringBuilder NameBuilder => field ??= new();
        private (string Name, string Value)? _attribute = null;
        private Dictionary<string, string?> Attributes => field ??= new();
        public Builder AppendToName(char @char) { NameBuilder.Append(@char); return this; }
        public Builder StartAttribute() { _attribute = (string.Empty, string.Empty); return this; }
        public bool SelfClosing { get; set; } = false;

        public Builder AppendAttributeName(char value)
        {
            if (_attribute is null) throw new InvalidOperationException("Cannot append to attribute name - attribute not started.");
            _attribute = (_attribute.Value.Name + value, _attribute.Value.Value);
            return this;
        }

        public Builder AppendAttributeValue(char value)
        {
            if (_attribute is null) throw new InvalidOperationException("Cannot append to attribute value - attribute not started.");
            _attribute = (_attribute.Value.Name, _attribute.Value.Value + value);
            return this;
        }

        public StartTagToken Build() => new(NameBuilder.ToString());
    }
}

public record CommentToken(string Data) : Token
{
    public sealed class Builder() : IBuilder<CommentToken>
    {
        public StringBuilder Data { get; set; } = new();

        public CommentToken Build() => new(Data.ToString());
    }
}

public record EndTagToken(string Name) : Token
{
    public sealed class Builder : IBuilder<EndTagToken>
    {
        private StringBuilder NameBuilder => field ??= new();
        public Builder AppendToName(char @char) { NameBuilder.Append(@char); return this; }
        public EndTagToken Build() => new(NameBuilder.ToString());
    }
}

public record CharacterToken(char Character) : Token;
public record EndOfFileToken : Token;