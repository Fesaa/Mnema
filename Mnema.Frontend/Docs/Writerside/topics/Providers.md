# Providers

Mnema supports downloading from a select few sources (providers). These are 100% based on whatever I need to download from,
you are however free to PR in support for any at any time. 

## Supported Providers

<warning>
    Kagane and Webtoons sleep 100ms between recently updated scans. If you have an insane amount of subscription on them, this could cause issues.
    Must be less than 9.000 combined, or the next sync will happen before the last finished.
</warning>

### Nyaa

Downloading from Nyaa requires a download client (QBit) setup, and will parse releases for matching `Valid Titles` in monitored series.
It is recommended you have `Hardcover` and/or `Mangabaka` ids linked. Leave external id empty

### Mangadex

Everything is supported.

### Webtoons

Everything is supported. Just two remarks, loading info for fresh downloads is super slow due to their pagination. Recently updated is fake
and simply loads latest uploads for each monitored series

### Dynasty

Everything is supported.

### Bato

Bato is dead, will not work

### Weebdex

Weebdex is dead, will not work

### Comix

Everything works, they something add cloudflare protection on their API. During this time it won't work

### Kagane

Everything works. One small remark, Kagane does not expose which chapter caused their updated_at or even a timestamp for updated at. Recently updated is thus fake
and simply loads latest uploads for each monitored series

<warning>
    You are required to include a WVD file in base64 format at `Authentication.Kagane` to download
</warning>

### How to add to support for a new provider

1) Update the Provider enums, and the provider-pipe
2) In `Mnema.Provider` create a new directory with the name of your website and
    - Create a class implementing `IRepository`. You can extend `AbstractRepository` if you're communicating with an api for QOL methods. Look at others for examples.
    - Implement all methods (ContentReleases are **required** to have ReleaseId set)
    - Update `ServiceProviderExtensions` in the same project. Look at the others for what's needed
3) Everything will now magically work and update
