# Mnema

Mnema is a (very) opinionated self-hosted automation tool to acquire media. With a focus on metadata and [Kavita](https://github.com/Kareadita/Kavita) integration.

Automate downloads from various sources with a ton of download options, enrich the content with metadata for seamless ingestion into Kavita while keeping
up to date with notifications.

![overview_monitored_series_screen.png](overview_monitored_series_screen.png)

## Requirements

As Mnema is opinionated as first as foremost made for my setup, as such will only be provided as a container.
First, and primary support (from me) goes to the full setup with postgres, oidc, and redis. However, it is possible to run Mnema
fully standalone without authentication and sqlite instead.


## Glossary

Provider
: The website/service where Mnema gets the information to download the content from (Mangadex, ...)

Metadata provider
: Third party websites you can use to enrich a providers provided metadata (Hardcover, Mangabaka)

Page
: A page is set up for a single provider, and allows you to search & filter their offers. You can then start a one-off download
or monitor the series

Monitored Series
: A monitored series either depends on a specific series from a provider (Weebdex, ...), or has valid titles setup which Mnema
uses to parse (Nyaa)
