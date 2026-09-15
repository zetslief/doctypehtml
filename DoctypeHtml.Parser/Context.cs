using System.Diagnostics.CodeAnalysis;

namespace DoctypeHtml.Parser;

internal class Context(ReadOnlyMemory<char> content, Action<Token> emitCallback)
{
    private readonly ReadOnlyMemory<char> _content = content;
    private readonly Action<Token> _onEmit = emitCallback;
    private int _cursor = 0;

    public bool EndOfContent => _content.Length <= _cursor;
    public Tokenizer.State State { get; set; } = Tokenizer.State.Data;
    public Tokenizer.State? ReturnState { get; set; } = null;
    public IBuilder? CurrentTokenBuilder { get; set; } = null;

    public TBuilder GetCurrentTokenBuilder<TBuilder>() where TBuilder : class, IBuilder
        => CurrentTokenBuilder as TBuilder ?? throw new InvalidOperationException($"Invalid builder. Expected start tag token builder. Current: {CurrentTokenBuilder}. State: {State}");

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

    public void ReconsumeInState(Tokenizer.State state)
    {
        _cursor--;
        State = state;
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