namespace DoctypeHtml.Tests;

public class TokenizerTests
{
    [Test]
    public void Basic()
    {
        var content = File.ReadAllText("./TestData/Basic.html");
        Console.WriteLine(content);
        Assert.Fail("Not implemented");
    }
}
