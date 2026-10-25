namespace HtmlT.Tests;

public class BasicTests
{
    const string BasicHtml = """
    <html>
        <htmlt-component-example />
        <input value="hello" />
        <p class="hello">Hello</p>
    </html>
    """;

    const string ComponentExample = """
    <div>
        <h1>Hello, HTML!</h1>
    </div>
    """;

    const string Expectation = """
    <html>
        <div>
        <h1>Hello, HTML!</h1>
    </div>
        <input value='hello'/>
        <p class='hello'>Hello</p>
    </html>
    """;

    [Test]
    public async Task BasicComponent()
    {
        var rootComponent = ComponentBuilder.FromHtml(BasicHtml)
            .AddChild("htmlt-component-example", ComponentBuilder.FromHtml(ComponentExample).Build())
            .Build();
        var htmlTemplate = new HtmlTemplate();
        var page = htmlTemplate.Render(rootComponent);
        Console.WriteLine(page);
        await Assert.That(page).IsEqualTo(Expectation);
    }
}
