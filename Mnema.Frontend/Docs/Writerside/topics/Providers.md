# Providers

Mnema supports downloading from a select few sources (providers). These are 100% based on whatever I need to download from,
you are however free to PR in support for any at any time.

## Provider Settings

You can fully disable a provider from allowing automatic downloads (skips in Monitored Series). Or require manual confirmation
before a download starts. These (and maybe more in the future) can be configured form the provider settings page.

## Supported Providers

### Nyaa

Downloading from Nyaa requires a download client (QBit) setup, and will parse releases for matching `Valid Titles` in monitored series.
It is recommended you have `Hardcover` and/or `Mangabaka` ids linked. Leave external id empty

Torrents matching to a list of naming conversions will be considered a grouped release. And will parse the files inside
as separate series. 

### Mangadex

Everything is supported.

### Webtoons

Everything is supported. Just two remarks, loading info for fresh downloads is super slow due to their pagination. Recently updated is fake
and simply loads latest uploads for each monitored series

### Dynasty

Everything is supported. Chapters can also be downloaded on their own (OneShots support).

### Madokami

Supported, with enriched metadata from Mangabaka & Hardcover. No metadata is taken from Madokami apart from the titles. 

Each directory is a separate series, Mnema will <strong>NOT</strong> recursive traverse directories at any point. Files in the directory are downloaded
one by one. And then processed once they're all downloaded.

It is important you select the correct (content) format for all features to work. It is recommended to assign a metadata id.

<warning>
    You must provide your credentials as a download client
</warning>

### How to add to support for a new provider

1) Update the Provider enums, and the provider-pipe
2) In `Mnema.Provider` create a new directory with the name of your website and
    - Create a class implementing `IRepository`. You can extend `AbstractRepository` if you're communicating with an api for QOL methods. Look at others for examples.
    - Implement all methods (ContentReleases are **required** to have ReleaseId set)
    - Update `ServiceProviderExtensions` in the same project. Look at the others for what's needed
3) Everything will now magically work and update
