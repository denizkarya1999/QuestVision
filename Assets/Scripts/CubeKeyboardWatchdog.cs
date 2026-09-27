/// <summary>Recovers if the native keyboard claims to be open without actually appearing.</summary>
public sealed class CubeKeyboardWatchdog
{
    public enum Recovery { None, DidNotOpen, ClosedWithoutResult }
    readonly double requestedAt;
    double lastPresentedAt;
    public bool WasPresented { get; private set; }

    public CubeKeyboardWatchdog(double now) { requestedAt = now; }

    public Recovery Observe(double now, bool presented, bool appHasInputFocus)
    {
        if (presented)
        {
            WasPresented = true;
            lastPresentedAt = now;
            return Recovery.None;
        }
        if (!WasPresented && now - requestedAt >= 5)
            return Recovery.DidNotOpen;
        if (WasPresented && appHasInputFocus && now - lastPresentedAt >= 1)
            return Recovery.ClosedWithoutResult;
        return Recovery.None;
    }
}
