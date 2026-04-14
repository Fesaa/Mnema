# Automatic Downloads

Downloading series is great, but automatically getting new releases downloaded is even better! 
Press the bell icon on a result to start monitoring it. More information about monitored series can be found here: 

<note>
    You can set up the same download options here, as with a manual download
</note>

<warning>
    Ensure <code>Format</code> and <code>Content Format</code> are correct, or files may not be imported 
</warning>

![monitored_series_new.png](monitored_series_new.png)

## How it works

Every 15m Mnema will retrieve all recently updated chapters/releases/... from every provider you're at least monitoring one series one.
For Nyaa it will check against Valid Titles, and the content of the torrent to decide if there's any new files to download. All others
will start a download if the series matches, and the chapter has not been imported yet.

You can check which items got imported via the releases settings page. Delete them to auto re-download (If the item is present in the next check.)
