# Monitored Series

Monitored series are the heart of your automated experience. Every 15m (not configurable) Mnema will load all recently
updated release (chapters/...) from the providers that have at least one monitored series. If there's any new content, 
it will start an automatic download with the options you've configured before.

![overview_monitored_series_screen.png](overview_monitored_series_screen.png)

## Series page

Each series gets its own entry, where you can view some of the relevant metadata and configure all its options. If the series
is linked to its Hardcover entry it'll also display upcoming volumes.

![monitored_series_series_page.png](monitored_series_series_page.png)

From this page you can decide to trigger a download, edit its options and other data, or preview its metadata.

<tip>
    Metadata is refreshed every 7 days, you may trigger the job early via the hangfire dashboard
</tip>

## Search series

Apart from adding monitored series from providers, you can also search a metadata provider for series you wish to monitor. 
These will use <code>Nyaa</code> to download from. You can search on [Hardcover](https://hardcover.app/) and [MangaBaka](https://mangabaka.org/)

![search-series.png](search-series.png)

## Calendar

Mnema provides a way to add upcoming releases to your favourite calendar app. If you have the required roles you can copy the url from the monitored series page.
This requires the `Host` to be set in application settings.
