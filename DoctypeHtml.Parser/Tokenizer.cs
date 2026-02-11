using System.Diagnostics.CodeAnalysis;

namespace DoctypeHtml.Parser;

public abstract record Token()
{
    internal static EndOfFileToken CreateEndOfFileToken() => new();
}
public record EndOfFileToken : Token;

internal class Context(ReadOnlyMemory<char> content, Action<Token> emitCallback)
{
    private readonly ReadOnlyMemory<char> _content = content;
    private readonly Action<Token> _onEmit = emitCallback;
    private int _cursor = 0;

    public bool EndOfContent => _content.Length - 1 <= _cursor;
    public Tokenizer.State State { get; set; } = Tokenizer.State.Data;

    public void Emit(Token token) => _onEmit(token);

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
            case '<':
                context.State = State.TagOpen;
                return;
            default:
                throw new NotImplementedException($"{nameof(ProcessData)} cannot handle '{currentInput}' yet.");
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
                context.State = State.MarkupDeclarationOpen;
                return;
            default:
                throw new NotImplementedException($"{nameof(ProcessData)} cannot handle '{currentInput}' yet.");
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
        while (context.TryConsumeNextInput(out var currentInput) && IsWhiteSpaceOrSeparator(currentInput.Value)) { /* ignoring */ }
        throw new NotImplementedException($"{nameof(ProcessBeforeDoctypeName)}: 'currentInput' {context}");
    }

    private static bool IsWhiteSpaceOrSeparator(char value) => value == ' ' || value == '\t' || value == '\u000A' || value == '\u000C';

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