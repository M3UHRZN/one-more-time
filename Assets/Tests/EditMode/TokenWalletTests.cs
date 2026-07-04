using NUnit.Framework;
using OneMoreTime;

public class TokenWalletTests
{
    [Test]
    public void Count_DefaultsToOne()
    {
        var wallet = new TokenWallet();

        Assert.AreEqual(1, wallet.Count);
    }

    [Test]
    public void Add_IncreasesCount()
    {
        var wallet = new TokenWallet();

        wallet.Add(2);

        Assert.AreEqual(3, wallet.Count);
    }

    [Test]
    public void TrySpend_WithTokens_DecrementsAndReturnsTrue()
    {
        var wallet = new TokenWallet();

        bool spent = wallet.TrySpend();

        Assert.IsTrue(spent);
        Assert.AreEqual(0, wallet.Count);
    }

    [Test]
    public void TrySpend_WithoutTokens_ReturnsFalseAndLeavesCountUnchanged()
    {
        var wallet = new TokenWallet();
        wallet.TrySpend(); // Count -> 0

        bool spent = wallet.TrySpend();

        Assert.IsFalse(spent);
        Assert.AreEqual(0, wallet.Count);
    }

    [Test]
    public void ResetToDefault_SetsCountBackToOne()
    {
        var wallet = new TokenWallet();
        wallet.Add(5);

        wallet.ResetToDefault();

        Assert.AreEqual(1, wallet.Count);
    }
}
