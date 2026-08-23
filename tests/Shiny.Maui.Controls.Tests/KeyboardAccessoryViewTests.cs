using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The bar hands every item it owns the field currently being edited. Items in the flat Items list
/// are trivial to find; items inside a <c>BarContent</c> layout are not, and an item the bar never
/// finds is one whose Owner stays null - which on screen is a Done button that does nothing.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class KeyboardAccessoryViewTests
{
    sealed class FakeHost : IKeyboardAccessoryHost
    {
        public FakeHost() => this.NavigationElement = new Entry();

        public VisualElement NavigationElement { get; }
        public bool Dismissed { get; private set; }
        public void DismissKeyboard() => this.Dismissed = true;
    }

    // Owner is protected on KeyboardAccessoryItem, so the assertions go through the one item whose
    // observable behaviour depends on it.
    sealed class ProbeItem : KeyboardAccessoryItem
    {
        public IKeyboardAccessoryHost? SeenOwner { get; private set; }
        public int OwnerChanges { get; private set; }

        protected internal override void OnOwnerChanged(IKeyboardAccessoryHost? owner)
        {
            this.SeenOwner = owner;
            this.OwnerChanges++;
        }
    }

    [Fact]
    public void Items_ReceiveTheFocusedOwner()
    {
        new Application();

        var probe = new ProbeItem();
        var bar = new KeyboardAccessoryView();
        bar.Items!.Add(probe);

        var host = new FakeHost();
        bar.NotifyFocusChanged(host, true);

        bar.CurrentOwner.ShouldBeSameAs(host);
        probe.SeenOwner.ShouldBeSameAs(host);

        bar.NotifyFocusChanged(host, false);
        probe.SeenOwner.ShouldBeNull();
    }

    [Fact]
    public void BarContent_ItemsAreFoundThroughNestedLayouts()
    {
        new Application();

        var probe = new ProbeItem();
        var content = new Grid
        {
            Children =
            {
                new ScrollView { Content = new HorizontalStackLayout { Children = { probe } } }
            }
        };

        var bar = new KeyboardAccessoryView { BarContent = content };

        var host = new FakeHost();
        bar.NotifyFocusChanged(host, true);

        probe.SeenOwner.ShouldBeSameAs(host);
    }

    [Fact]
    public void BarContent_DismissItemPutsTheKeyboardAway()
    {
        new Application();

        var dismiss = new KeyboardDismissItem();
        var bar = new KeyboardAccessoryView
        {
            BarContent = new Grid { Children = { dismiss } }
        };

        var host = new FakeHost();
        bar.NotifyFocusChanged(host, true);

        dismiss.Invoke();

        host.Dismissed.ShouldBeTrue();
    }
}
