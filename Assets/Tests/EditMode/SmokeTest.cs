using NUnit.Framework;

public class SmokeTest
{
    [Test]
    public void TestPipeline_Runs()
    {
        Assert.AreEqual(2, 1 + 1);
    }
}
