using System.Diagnostics.CodeAnalysis;
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
        public Builder AppendToName(char @char) { NameBuilder.Append(@char); return this; }
        public StartTagToken Build() => new(NameBuilder.ToString());
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

internal class Context(ReadOnlyMemory<char> content, Action<Token> emitCallback)
{
    private readonly ReadOnlyMemory<char> _content = content;
    private readonly Action<Token> _onEmit = emitCallback;
    private int _cursor = 0;

    public bool EndOfContent => _content.Length <= _cursor;
    public Tokenizer.State State { get; set; } = Tokenizer.State.Data;
    public IBuilder? CurrentTokenBuilder { get; set; } = null;

    public void Emit(Token token) => _onEmit(token);
    public void EmitCurrent() => _onEmit(CurrentTokenBuilder?.Build() ?? throw new InvalidOperationException("Cannot emit null token!"));

    public bool TryConsumeNextInput([NotNullWhen(true)] out char? character)
    {
        if (EndOfContent)
        {
            character = null;
            return false;
        }

        character = _content.Span[_cursor++];
        return true;
    }

    public void Reconsume() => _cursor--;

    public ReadOnlySpan<char> TryPeek(int count) => _content.Length > _cursor + count
        ? _content.Span[_cursor..(_cursor + count)]
        : [];

    public void Consume(int count)
    {
        if (_content.Length <= _cursor + count) throw new InvalidOperationException($"Consuming more than available.");
        _cursor += count;
    }

    public override string ToString() => $"Context ({_content.Length}): Cursor {_cursor}, State {State}";
}

public static class Tokenizer
{
    private const char ReplacementChar = '\uFFFD';

    public static void Run(ReadOnlyMemory<char> content, Action<Token> tokenEmitCallback)
    {
        Context context = new(content, tokenEmitCallback);
        do ProcessNextToken(context);
        while (!context.EndOfContent);
    }

    private static void ProcessNextToken(Context context)
    {
        switch (context.State)
        {
            case State.Data: ProcessData(context); break;
            case State.TagOpen: ProcessTagOpen(context); break;
            case State.MarkupDeclarationOpen: ProcessMarkupDeclarationOpen(context); break;
            case State.Doctype: ProcessDoctype(context); break;
            case State.BeforeDoctypeName: ProcessBeforeDoctypeName(context); break;
            case State.DoctypeName: ProcessDoctypeName(context); break;
            case State.TagName: ProcessTagName(context); break;
            case State.EndTagOpen: ProcessEndTagOpen(context); break;
            default: throw new NotImplementedException($"Unknown state: {context}");
        }
    }

    private static void ProcessData(Context context)
    {
        if (!context.TryConsumeNextInput(out var currentInput))
        {
            context.Emit(Token.CreateEndOfFileToken());
            return;
        }
        switch (currentInput)
        {
            case '&':
                throw new NotImplementedException($"{nameof(ProcessData)} cannot handle '{currentInput}' yet.");
            case '<':
                context.State = State.TagOpen; return;
            case '\u0000':
                // TODO: process parse error properly.
                context.Emit(new CharacterToken(currentInput.Value));
                return;
            default:
                context.Emit(new CharacterToken(currentInput.Value));
                break;
        }
    }

    private static void ProcessTagOpen(Context context)
    {
        if (!context.TryConsumeNextInput(out var currentInput))
        {
            context.Emit(Token.CreateEndOfFileToken());
            return;
        }
        switch (currentInput)
        {
            case '!':
                context.State = State.MarkupDeclarationOpen; return;
            case '/':
                context.State = State.EndTagOpen; return;
            case var asciiAlpha when char.IsAsciiLetter(currentInput.Value):
                context.CurrentTokenBuilder = new StartTagToken.Builder();
                context.State = State.TagName;
                context.Reconsume();
                break;
            default:
                throw new NotImplementedException($"{nameof(ProcessTagOpen)} cannot handle '{currentInput}' yet.");
        }
    }

    private static void ProcessMarkupDeclarationOpen(Context context)
    {
        const string doctype = "DOCTYPE";
        var maybeDoctype = context.TryPeek(doctype.Length);
        if (maybeDoctype.Equals(doctype.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
        {
            context.Consume(doctype.Length);
            context.State = State.Doctype;
            return;
        }
        else throw new NotImplementedException($"{nameof(ProcessMarkupDeclarationOpen)}");
    }

    private static void ProcessDoctype(Context context)
    {
        if (!context.TryConsumeNextInput(out var currentInput)) throw new NotImplementedException($"EOF in {nameof(ProcessDoctype)} is not implemented.");
        if (IsWhiteSpaceOrSeparator(currentInput.Value))
        {
            context.State = State.BeforeDoctypeName;
            return;
        }
        throw new NotImplementedException($"{nameof(ProcessDoctype)}: '{currentInput}' {context}");
    }

    private static void ProcessBeforeDoctypeName(Context context)
    {
        var success = context.TryConsumeNextInput(out var maybeCurrentInput);
        while (success && IsWhiteSpaceOrSeparator(maybeCurrentInput!.Value))
        {
            success = context.TryConsumeNextInput(out maybeCurrentInput);
        }
        if (!success) throw new NotImplementedException($"{nameof(ProcessBeforeDoctypeName)}: '{maybeCurrentInput}' {context}");
        var currentInput = maybeCurrentInput!.Value;
        if (char.IsAsciiLetterUpper(currentInput))
        {
            context.CurrentTokenBuilder = new DoctypeToken.Builder().AppendToName(currentInput);
            context.State = State.DoctypeName;
        }
        else if (IsNull(currentInput))
        {
            context.CurrentTokenBuilder = new DoctypeToken.Builder().AppendToName('\uFFFD');
            context.State = State.DoctypeName;
        }
        else if (currentInput == '>') throw new NotImplementedException($"{nameof(ProcessBeforeDoctypeName)}: handling '>' token.");
        else
        {
            context.CurrentTokenBuilder = new DoctypeToken.Builder().AppendToName(currentInput);
            context.State = State.DoctypeName;
        }
    }

    private static void ProcessDoctypeName(Context context)
    {
        if (context.CurrentTokenBuilder is not DoctypeToken.Builder doctypeTokenBuilder) throw new InvalidOperationException($"Doctype token builder is not present.");
        if (!context.TryConsumeNextInput(out var maybeCurrentInput)) throw new NotImplementedException($"EOF in {nameof(ProcessDoctypeName)} is not implemented.");
        var currentInput = maybeCurrentInput.Value;
        if (IsWhiteSpaceOrSeparator(currentInput))
        {
            context.State = State.AfterDoctypeName;
        }
        else if (currentInput == '>')
        {
            context.State = State.Data;
            context.EmitCurrent();
        }
        else if (char.IsAsciiLetterUpper(currentInput))
        {
            doctypeTokenBuilder.AppendToName(char.ToLowerInvariant(currentInput));
        }
        else if (IsNull(currentInput))
        {
            // TODO: handle parse error.
            doctypeTokenBuilder.AppendToName(ReplacementChar);
        }
        else
        {
            doctypeTokenBuilder.AppendToName(currentInput);
        }
    }

    private static void ProcessTagName(Context context)
    {
        if (!context.TryConsumeNextInput(out var maybeCurrentInput)) throw new NotImplementedException($"EOF in {nameof(ProcessDoctypeName)} is not implemented.");
        var currentInput = maybeCurrentInput.Value;
        char? addToBuilder = null;
        if (IsWhiteSpaceOrSeparator(currentInput)) context.State = State.BeforeAttributeName;
        else if (char.IsAsciiLetterUpper(currentInput)) addToBuilder = char.ToLowerInvariant(currentInput);
        else if (IsNull(currentInput))
        {
            // TODO: process parse error properly.
            addToBuilder = ReplacementChar;
        }
        else if (currentInput == '>')
        {
            context.State = State.Data;
            context.EmitCurrent();
        }
        else addToBuilder = currentInput;
        if (addToBuilder is null) return;
        switch (context.CurrentTokenBuilder)
        {
            case StartTagToken.Builder startBuilder: startBuilder.AppendToName(addToBuilder.Value); break;
            case EndTagToken.Builder endBuilder: endBuilder.AppendToName(addToBuilder.Value); break;
            default: throw new InvalidOperationException("Start tag token builder is not present.");
        }
    }

    private static void ProcessEndTagOpen(Context context)
    {
        if (!context.TryConsumeNextInput(out var maybeCurrentInput)) throw new NotImplementedException($"EOF in {nameof(ProcessDoctypeName)} is not implemented.");
        var currentInput = maybeCurrentInput.Value;
        if (char.IsAsciiLetter(currentInput))
        {
            context.CurrentTokenBuilder = new EndTagToken.Builder();
            context.Reconsume();
            context.State = State.TagName;
        }
        else if (currentInput == '>')
        {
            // TODO: process parse error properly.
            context.State = State.Data;
        }
        else
        {
            // TODO: This is an invalid-first-character-of-tag-name parse error. Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            throw new NotImplementedException($"{nameof(ProcessEndTagOpen)}: '{currentInput}' {context}");
        }
    }

    private static bool IsWhiteSpaceOrSeparator(char value) => value == ' ' || value == '\t' || value == '\u000A' || value == '\u000C';
    private static bool IsNull(char value) => value == '\u0000';

    public enum State
    {
        Data,
        RcDatA,
        RawText,
        ScriptData,
        PlainText,
        TagOpen,
        EndTagOpen,
        TagName,
        RcDataLessThanSign,
        RcDataEndTagOpen,
        RcDataEndTagName,
        RawTextLessThanSign,
        RawTextEndTagOpen,
        RawTextEndTagName,
        ScriptDataLessThanSign,
        ScriptDataEndTagOpen,
        ScriptDataEndTagName,
        ScriptDataEscapeStart,
        ScriptDataEscapeStartDash,
        ScriptDataEscaped,
        ScriptDataEscapedDash,
        ScriptDataEscapedDashDash,
        ScriptDataEscapedLessThanSign,
        ScriptDataEscapedEndTagOpen,
        ScriptDataEscapedEndTagName,
        ScriptDataDoubleEscapeStart,
        ScriptDataDoubleEscaped,
        ScriptDataDoubleEscapedDash,
        ScriptDataDoubleEscapedDashDash,
        ScriptDataDoubleEscapedLessThanSign,
        ScriptDataDoubleEscapeEnd,
        BeforeAttributeName,
        AttributeName,
        AfterAttributeName,
        BeforeAttributeValue,
        AttributeValueDoubleQuoted,
        AttributeValueSingleQuoted,
        AttributeValueUnquoted,
        AfterAttributeValueQuoted,
        SelfClosingStartTag,
        BogusComment,
        MarkupDeclarationOpen,
        CommentStart,
        CommentStartDash,
        Comment,
        CommentLessThanSign,
        CommentLessThanSignBang,
        CommentLessThanSignBangDash,
        CommentLessThanSignBangDashDash,
        CommentEndDash,
        CommentEnd,
        CommentEndBang,
        Doctype,
        BeforeDoctypeName,
        DoctypeName,
        AfterDoctypeName,
        AfterDoctypePublicKeyword,
        BeforeDoctypePublicIdentifier,
        DoctypePublicIdentifierDoubleQuoted,
        DoctypePublicIdentifierSingleQuoted,
        AfterDoctypePublicIdentifier,
        BetweenDoctypePublicAndSystemIdentifiers,
        AfterDoctypeSystemKeyword,
        BeforeDoctypeSystemIdentifier,
        DoctypeSystemIdentifierDoubleQuoted,
        DoctypeSystemIdentifierSingleQuoted,
        AfterDoctypeSystemIdentifier,
        BogusDoctype,
        CdataSection,
        CdataSectionBracket,
        CdataSectionEnd,
        CharacterReference,
        NamedCharacterReference,
        AmbiguousAmpersand,
        NumericCharacterReference,
        HexadecimalCharacterReferenceStart,
        DecimalCharacterReferenceStart,
        HexadecimalCharacterReference,
        DecimalCharacterReference,
        NumericCharacterReferenceEnd,
    }
}