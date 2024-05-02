using LMCore.Extensions;
using NUnit.Framework;

public class TestScalarExtensions
{
    [Test]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(-1, -1)]
    [TestCase(-3, -1)]
    public void TestSign(int value, int sign)
    {
        Assert.That(value.Sign(), Is.EqualTo(sign));
    }
}