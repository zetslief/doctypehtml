namespace HtmlT.Tests;

public sealed class BuilderTests
{
    [Test]
    public async Task CssTestAsync()
    {
        const string first = """
        p {
            background-color: white;
        }
        """;
        const string second = """
        p {
            background-color: black;
        }
        """;
        const string expected = """
        p {
            background-color: white;
        }
        p {
            background-color: black;
        }
        """;
        var result = ComponentBuilder.FromCss(first, second).Build();
        await Assert.That(result).IsOfType(typeof(CssComponent));
        await Assert.That(((CssComponent)result).Content).IsEqualTo(expected);
    }

    [Test]
    public async Task JsTestAsync()
    {
        const string first = """
        console.log("hello");
        """;
        const string second = """
        console.log("world");
        """;
        const string expected = """
        console.log("hello");
        console.log("world");
        """;
        var result = ComponentBuilder.FromJs(first, second).Build();
        await Assert.That(result).IsOfType(typeof(JsComponent));
        await Assert.That(((JsComponent)result).Content).IsEqualTo(expected);
    }
}