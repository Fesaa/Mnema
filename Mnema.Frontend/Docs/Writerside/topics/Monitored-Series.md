# Monitored Series

![overview_monitored_series_screen.png](overview_monitored_series_screen.png)

## Series page

Each series gets its own entry, where you can view some of the relevant metadata and configure all its options. If the series
is linked to its Hardcover or Mangabaka entry it'll also display upcoming volumes.

![monitored_series_series_page.png](monitored_series_series_page.png)

From this page you can decide to trigger a download, edit its options and other data, or preview its metadata.

<tip>
    Metadata is refreshed every 7 days, you may trigger the job early via the hangfire dashboard. Or click on the last refresh date to refresh a specific series
</tip>

## Chapters

Everything is considered a chapter, so for volume release you'll find them here too. You can view and edit all chapters here

The chapters tab lists every chapter Mnema knows about, with its status. Switch it to `Not Monitored` to prevent it from being downloaded in the future.

* A volume file, such as `Series Vol. 1.cbz`, covers every chapter in that volume.
* A range file, such as `Chapter 1-10.cbz`, covers every chapter in the range.

![chapters_toolbar.png](chapters_toolbar.png)

### Volumes

The Volumes tab groups the same chapters by volume. Use it when you want to work on a whole volume at once. Each group shows the volume's cover, the number of chapters, and how many are available

![volume_tab.png](volume_tab.png)

### Covers

Chapter thumbnails are read from the archive when the chapter is available on disk

You can replace/update the cover of an archive chapter from the chapters tab:

* Click the image icon on one chapter to update only that chapter.
* Select multiple chapters with the checkboxes, then click **Update covers**.

Covers are collected from the provider, when the provider supports covers (currently Mangadex), and from the linked MangaBaka and Hardcover entries. If a cover matching the chapter's volume exists it is applied directly, otherwise you choose one from the list.

From the Volumes tab, the **Update cover** button does the same for every available archive chapter in the volume.

<note>
    Bulk cover updates require all selected chapters to be in the same volume. Only archive chapters (`.cbz`, `.zip`) that are available on disk can be updated.
</note>

### Combining chapters into a volume

You can start this from the Chapters tab by selecting two or more chapters, or from the Volumes tab with the volume's **Combine into volume** button. The volume button selects every chapter file in that volume.

Mnema creates one file named `Title Vol. N.cbz` next to the chapter files, writes a single ComicInfo with the volume and the chapter range, and deletes the original chapter files.

The action requires:

* The series format must be Archive.
* At least two chapter files.
* All chapters in the same volume, with a volume marker.
* Every selected chapter must be available on disk. The Volumes tab button requires every chapter in the volume to be available.

<warning>
    The original chapter files are deleted. The selected chapter entries all point to the combined file afterwards.
</warning>


## Search series

Apart from adding monitored series from providers, you can also search a metadata provider for series you wish to monitor. 
These will use <code>Nyaa</code> to download from. You can search on [Hardcover](https://hardcover.app/) and [MangaBaka](https://mangabaka.org/)

![search-series.png](search-series.png)

## Calendar

Mnema provides a way to add upcoming releases to your favourite calendar app. If you have the required roles you can copy the url from the monitored series page.
This requires the `Host` to be set in application settings.
