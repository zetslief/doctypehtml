namespace HtmlT.Tests;

public class BasicTests
{
    const string BasicHtml = """
    <html>
        <htmlt-component-example />
        <input id-example="id-value" value="hello" />
        <input name="empty-attribute-name" checked-true-value />
        <input name="empty-attribute-name" checked-false-value />
        <p class="hello"><hellotext /></p>
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
        <input id-example-expected='id-value-expected' value='hello' />
        <input name='empty-attribute-name' checked />
        <input name='empty-attribute-name' />
        <p class='hello'>Hello</p>
    </html>
    """;

    [Test]
    public async Task BasicComponent()
    {
        var rootComponent = ComponentBuilder.FromHtml(BasicHtml)
            .AddChild("htmlt-component-example", ComponentBuilder.FromHtml(ComponentExample).Build())
            .AddChild("hellotext", ComponentBuilder.Text("Hello"))
            .AddData("id-example", "id-example-expected")
            .AddData("id-value", "id-value-expected")
            .AddData("checked-true-value", "checked")
            .AddData("checked-false-value", string.Empty)
            .Build();
        var htmlTemplate = new HtmlTemplate();
        var page = htmlTemplate.Render(rootComponent);
        Console.WriteLine(page);
        await Assert.That(page).IsEqualTo(Expectation);
    }
}
