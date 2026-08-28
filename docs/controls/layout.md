# Layout & AppLayout (Blazor)

[← All Shiny Controls](../../README.md)

Layout primitives and an application shell, Blazor only — MAUI already has `VerticalStackLayout`, `HorizontalStackLayout` and `Grid` in the box.

**`VStack` / `HStack`** — flexbox stacks with `Spacing` (px), `Align`, `Justify`, `Wrap`, `Reverse`, `Grow`, `Padding`, `Background` and `Scrollable`. Everything is an inline style, so there is no stylesheet to load.

```razor
<VStack Spacing="12" Align="StackAlign.Start" Padding="16">
    <h3>Title</h3>
    <HStack Spacing="8" Justify="StackJustify.SpaceBetween" Align="StackAlign.Center">
        <span>Label</span>
        <ShinyButton Text="Save" />
    </HStack>
</VStack>
```

**`Grid` / `Row` / `Column`** — a responsive 12-column grid. Each `Column` takes a span per breakpoint (`Xs` &lt; 576px, `Sm` ≥ 576, `Md` ≥ 768, `Lg` ≥ 992, `Xl` ≥ 1200, `Xxl` ≥ 1400) that cascades upwards, so `Md="6"` alone means full width on phones and half from 768px up. `OffsetXs…OffsetXxl` and `OrderXs…OrderXxl` are per-breakpoint too; a `Column` with no span shares the row equally with its siblings, and `Fit` shrinks to its content. `Columns`, `Gutter`/`GutterX`/`GutterY` and `MaxWidth` are configurable on the `Grid` and overridable per `Row`.

```razor
<Grid Gutter="16" MaxWidth="1200">
    <Row>
        <Column Md="8"><Article /></Column>
        <Column Md="4" OrderXs="1" OrderMd="2"><Sidebar /></Column>
    </Row>
</Grid>
```

**`AppLayout`** — an application shell of a header, footer, left and right panels and the content between them. Regions are placed by CSS grid areas, so they can appear in any order in the markup, and each owns its own scroll region.

```razor
<AppLayout Height="100dvh" HeaderSpan="LayoutSpan.Full" BorderWidth="1">
    <AppLayoutHeader Height="56" Padding="0 16">…</AppLayoutHeader>

    <AppLayoutPanel Side="PanelSide.Left"
                    @bind-State="leftState"
                    @bind-Size="leftWidth"
                    MinSize="180" MaxSize="420"
                    ToolbarSize="56"
                    CollapseBelow="900"
                    CollapsedState="PanelState.Toolbar"
                    PersistKey="nav">
        <HeaderContent>…</HeaderContent>
        <ToolbarContent>…</ToolbarContent>
        <ChildContent>…</ChildContent>
        <FooterContent>…</FooterContent>
    </AppLayoutPanel>

    <AppLayoutContent Padding="20">@Body</AppLayoutContent>
    <AppLayoutFooter Height="36">…</AppLayoutFooter>
</AppLayout>
```

- **Three panel states** — `PanelState.Hidden`, `Toolbar` (a narrow rail rendering `ToolbarContent`, configurable to whatever you want) and `Shown`. Two-way bindable via `@bind-State`, or driven from code with `SetStateAsync` / `ToggleAsync` on a `@ref`
- **Resizing** — drag the handle on the panel's inner edge; the width is clamped to `MinSize`/`MaxSize` and reported back through `@bind-Size`. Set `Resizable="false"` to pin it
- **Scroll regions** — the panel body, the toolbar rail and the content each scroll on their own, while `HeaderContent`/`FooterContent`, the shell header and the shell footer stay pinned
- **Borders** — `BorderWidth`/`BorderColor` on the `AppLayout` set the default for every region; each region overrides them or turns its divider off with `Border="false"`. Unset values resolve through CSS variables, so theme tokens still apply. A `Hidden` panel drops its divider entirely, so a collapsed panel leaves no sliver butting up against the next region's border
- **Responsive** — under `CollapseBelow` (measured on the shell, not the window) an expanded panel drops to `CollapsedState`, and re-expanding it there floats it over the content as a drawer with a scrim
- **Persistence** — set `PersistKey` and the panel's state and width are saved to localStorage and restored on load
- **`HeaderSpan`/`FooterSpan`** — `LayoutSpan.Full` runs the header/footer the full width, `LayoutSpan.Content` insets it between the panels so they run the full height
