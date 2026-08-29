// Docking, the tray icon and quick entry's hotkeys are API rather than markup, so this package maps
// no XAML namespaces of its own. The ribbon used to live here and was mapped from this file; it moved
// into the core package so that ImageEditor - which targets iOS and Android, where this package does
// not build - could use it, and core's GlobalXmlns maps it now. The URI never changed, so a XAML
// author still writes `shiny:Ribbon` and nothing in markup moved.
