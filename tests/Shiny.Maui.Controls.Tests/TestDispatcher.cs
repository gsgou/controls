using Microsoft.Maui.Dispatching;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Enough of a dispatcher to construct controls that queue work onto one. Everything dispatched
/// runs inline; timers are created but never tick, so a test that needs a timer to fire has to
/// drive it itself rather than race the clock.
/// </summary>
sealed class TestDispatcher : IDispatcher
{
    public bool IsDispatchRequired => false;

    public bool Dispatch(Action action)
    {
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        action();
        return true;
    }

    public IDispatcherTimer CreateTimer() => new InertTimer();

    sealed class InertTimer : IDispatcherTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public bool IsRunning { get; private set; }

        public event EventHandler? Tick;

        public void Start() => this.IsRunning = true;

        public void Stop()
        {
            this.IsRunning = false;
            _ = this.Tick;   // the event exists for the interface; nothing here raises it
        }
    }
}


sealed class TestDispatcherProvider : IDispatcherProvider
{
    static readonly TestDispatcher Instance = new();

    public IDispatcher? GetForCurrentThread() => Instance;

    /// <summary>Idempotent - xUnit runs the classes that need this in the same process.</summary>
    public static void Install() => DispatcherProvider.SetCurrent(new TestDispatcherProvider());
}
