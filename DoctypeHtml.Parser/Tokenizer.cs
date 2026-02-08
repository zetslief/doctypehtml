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
}

public static class Tokenizer
{
    public static void Run(ReadOnlyMemory<char> content, Action<Token> tokenEmitCallback)
    {
        Context context = new(content, tokenEmitCallback);
        do ProcessNextToken(context);
        while (context.EndOfContent);
    }
    
    private static void ProcessNextToken(Context context)
    {
        switch (context.State) 
        {
            case State.Data: ProcessDataState(context); break;
            case State.TagOpen: ProcessTagOpenState(context); break;
            default: throw new NotImplementedException($"{context}");
        };
    }
    
    private static void ProcessDataState(Context context)
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
                throw new NotImplementedException($"{nameof(ProcessDataState)} cannot handle '{currentInput}'({(int)currentInput}) yet");
        }
    }
    
    private static void ProcessTagOpenState(Context context)
    {
        if (!context.TryConsumeNextInput(out var currentInput))
        {
            context.Emit(Token.CreateEndOfFileToken());
            return;
        }
    }
    
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
