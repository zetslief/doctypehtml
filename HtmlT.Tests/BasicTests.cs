using System.ComponentModel;
using System.Text;

namespace HtmlT.Tests;

public class BasicTests
{
    const string BasicHtml = """
    <html>
        <htmlt-component-example />
        <input value="hello" />
    </html>
    """;

    const string ComponentExample = """
    <div>
        <h1>Hello, HTML!</h1>
    </div>
    """;

    [Test]
    public async Task BasicComponent()
    {
        var rootComponent = new HtmlT.Component(BasicHtml, new Dictionary<string, Component>
        {
            { "htmlt-component-example", new Component(ComponentExample, []) },
        });
        var htmlTemplate = new HtmlT.HtmlTemplate();
        var page = htmlTemplate.Render(rootComponent);
        Console.WriteLine(page);
    }
}
