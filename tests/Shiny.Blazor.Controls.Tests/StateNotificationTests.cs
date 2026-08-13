using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// A state notifies its host from <c>OnParametersSet</c>, and the host answers by re-rendering — which
/// sets the state's parameters again. If the notification is unconditional that is an infinite render
/// loop: the Blazor wizard page pinned a CPU core and never painted. These pin the gate that stops it.
/// </summary>
public class StateNotificationTests
{
    [Fact]
    public void AParameterSetThatChangedNothingIsSilent()
    {
        var host = new CountingHost();
        var state = new TestState { Owner = host, Name = "Loading" };

        state.Init();
        state.SetParameters();
        state.SetParameters();
        state.SetParameters();

        // The first one is the state announcing its name; the re-renders it causes must not answer back.
        host.Notified.ShouldBe(1);
    }

    [Fact]
    public void ChangingTheNameNotifiesAgain()
    {
        var host = new CountingHost();
        var state = new TestState { Owner = host, Name = "Loading" };

        state.Init();
        state.SetParameters();
        state.Name = "Loaded";
        state.SetParameters();

        host.Notified.ShouldBe(2);
    }

    [Fact]
    public void AWizardStepIsSilentWhenNoneOfItsRulesMoved()
    {
        var host = new CountingHost();
        var step = new TestStep { Owner = host, Name = "Delivery", Title = "Delivery" };

        step.Init();
        step.SetParameters();
        step.SetParameters();
        step.SetParameters();

        host.Notified.ShouldBe(1);
    }

    [Theory]
    [InlineData("IsVisible")]
    [InlineData("IsEnabled")]
    [InlineData("IsValid")]
    [InlineData("IsOptional")]
    [InlineData("IsCompleted")]
    [InlineData("Title")]
    [InlineData("NextText")]
    public void AWizardStepSpeaksUpWhenSomethingTheWizardDrawsMoves(string property)
    {
        var host = new CountingHost();
        var step = new TestStep { Owner = host, Name = "Delivery", Title = "Delivery" };

        step.Init();
        step.SetParameters();
        host.Notified.ShouldBe(1);

        switch (property)
        {
            case "IsVisible": step.IsVisible = false; break;
            case "IsEnabled": step.IsEnabled = false; break;
            case "IsValid": step.IsValid = false; break;
            case "IsOptional": step.IsOptional = true; break;
            case "IsCompleted": step.IsCompleted = true; break;
            case "Title": step.Title = "Ship to"; break;
            case "NextText": step.NextText = "Place order"; break;
        }

        step.SetParameters();
        host.Notified.ShouldBe(2);

        // And then settles again rather than notifying on every pass from here on.
        step.SetParameters();
        host.Notified.ShouldBe(2);
    }

    [Fact]
    public void AStateWithNoHostDoesNotThrow()
    {
        var state = new TestState { Name = "Orphan" };

        state.Init();
        Should.NotThrow(state.SetParameters);
    }


    class CountingHost : IStateViewHost
    {
        public int Registered { get; private set; }
        public int Notified { get; private set; }

        public void RegisterState(StateViewState state) => this.Registered++;
        public void UnregisterState(StateViewState state) { }
        public void NotifyStateChanged(StateViewState state) => this.Notified++;
    }

    // ComponentBase keeps its lifecycle methods protected, so the gate is driven through a double
    // rather than a renderer - these are about what the state says, not about how it is hosted.
    class TestState : StateViewState
    {
        public void Init() => this.OnInitialized();
        public void SetParameters() => this.OnParametersSet();
    }

    class TestStep : WizardStep
    {
        public void Init() => this.OnInitialized();
        public void SetParameters() => this.OnParametersSet();
    }
}
