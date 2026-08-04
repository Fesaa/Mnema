# Server

There's a few general settings to limit max concurrent downloads. The important part however are the metadata settings. 

Generally speaking, metadata from metadata providers should be of higher quality than that of providers (called upstream), 
however in practise it really depend on which. 

You can decide which metadata to use from where, and in which order to merge items. Items at the top of the list go first, if the first
one has a summary the others' summary won't be used

![server-settings-metadata-providers.png](server-settings-metadata-providers.png)

## Mangabaka

Mangabaka has a ton of Metadata, not all of it may interest you. Tags & Genres can be configured in [](Preferences.md), the following extra options are avaible.

![mangabaka-metadata-settings.png](mangabaka-metadata-settings.png)

### Series Name and Localized Series Name

For both (ComicInfo) fields you can supply a comma separated list of languages to use for them, the native language of the series can be supplied via the [formatting system](Naming.md). They're processed in order.

The following input would English, French, Romanised Native title, then the Native Title.
```
en, fr, {Native}-latn, {Native}
```

### Filter Weblinks

A lot of links are provided, to keep your web links clean you can filter out those you want by supplying a list of filter rules. 

Each rule is set to `Include` or `Exclude`. A link is included if it matches no rules or at least one `Include` rule. (I.e. `Include` is stronger than `Exclude`).

You can filter on the following attributes

| Type     | Input                                       |
|----------|---------------------------------------------|
| Hostname | hostname to filter, ignored www. subdomain. |
| Language | language of the title (Supports Native)     |
