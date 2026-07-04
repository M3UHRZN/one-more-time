using NUnit.Framework;
using OneMoreTime;

public class RunTimerTests
{
    [Test]
    public void Begin_ResetsAndStartsRunning()
    {
        var timer = new RunTimer();

        timer.Begin();

        Assert.IsTrue(timer.IsRunning);
        Assert.AreEqual(0f, timer.Elapsed);
    }

    [Test]
    public void Tick_AccumulatesOnlyWhileRunning()
    {
        var timer = new RunTimer();
        timer.Begin();

        timer.Tick(0.5f);
        timer.Tick(0.25f);

        Assert.AreEqual(0.75f, timer.Elapsed, 0.0001f);

        timer.Stop();
        timer.Tick(10f);

        Assert.AreEqual(0.75f, timer.Elapsed, 0.0001f, "Stopped timer must not accumulate.");
        Assert.IsFalse(timer.IsRunning);
    }

    [Test]
    public void Begin_AfterStop_ResetsElapsedAndResumes()
    {
        var timer = new RunTimer();
        timer.Begin();
        timer.Tick(5f);
        timer.Stop();

        timer.Begin();

        Assert.IsTrue(timer.IsRunning);
        Assert.AreEqual(0f, timer.Elapsed);
    }
}
