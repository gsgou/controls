# CountryPicker

[← All Shiny Controls](../../README.md)

A country search control built on AutoCompleteEntry with flag emoji display, country name, and dial code. Searches all ISO 3166-1 countries.

| Empty | With Selection |
|:---:|:---:|
| ![Country & Address](../../assets/countryaddress1.png) | ![Country Selected](../../assets/countryaddress2.png) |

```xml
<shiny:CountryPicker SelectedCountry="{Binding Country}"
                     Placeholder="Select country..."
                     FontSize="16"
                     TextColor="Black" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| SelectedCountry | Country | null | Selected country (TwoWay) |
| Placeholder | string | "Search countries..." | Placeholder text |
| MaxDropDownHeight | double | 200 | Max dropdown height |
| TextColor | Color/string | null | Text color |
| PlaceholderColor | Color/string | null | Placeholder color |
| DropDownBackgroundColor | Color/string | null | Dropdown background |
| DropDownBorderColor | Color/string | null | Dropdown border color |
| FontSize | double | 14 | Font size |
| FontFamily | string? | null | Font family (MAUI only) |
| CornerRadius | double | 4 | Dropdown corner radius (MAUI only) |
| InputClass | string? | null | Input CSS class (Blazor only) |
| DropDownClass | string? | null | Dropdown CSS class (Blazor only) |

Events: `CountrySelected` fires when a country is chosen.

The `Country` model provides: `Name`, `Iso2`, `Iso3`, `DialCode`, `FlagEmoji`.
