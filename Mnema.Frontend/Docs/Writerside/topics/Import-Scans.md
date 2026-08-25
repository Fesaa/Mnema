# Import Scans

You may already have had a library before you set up Mnema, to help you import your existing series Mnema can scan a directory for you.
After running a scan, you'll see a quick overview pop up on the import scan page. Showing you how many series you need to process.

![import-scans-menu.png](import-scans-menu.png)

## Scanner

The scanner is a very simple recursive process:

1) For the current directory, check if it contains any files at its root.
2) No ⇒ Get all subdirectories and repeat from step 1)
3) Yes ⇒ Assume this directory is a series and parse.

## Parser

The parser for each series is very, again, simple
1) Are all files in the directory of the same type (cbz or epub)
2) No ⇒ Error, Yes ⇒ continue
3) Parse metadata out of the first file (Series name falls back to directory name)
4) Parse upstream metadata ids and try to link to an existing monitored series
5) If a successful link, mark as imported otherwise as queued

After a scan has finished, you can open the scan in the UI. And complete the information for all found series.

![import-scan-directory-result.png](import-scan-directory-result.png)

### Actions

| Action      | When                                           | Result                                                            |
|-------------|------------------------------------------------|-------------------------------------------------------------------|
| Reject      | You do not want to import this series          | Moved to the back of the list, and marked in red                  |
| Skip        | You do not want to process this series now     | Moved to the back of the list, and marked in gray (before reject) |
| Auto Accept | The series has a Hardcover or Mangabaka id     | Create a monitored series automaticlly from the avaible metadata  |
| Accept      | Missing metadata, or in need of custom options | Open the monitor series modal for full customizability            |


### Errors

![import-scans-errors.png](import-scans-errors.png)

When a series cannot be imported, an error will appear. 

| Action      | When                                                      | Result                                                       |
|-------------|-----------------------------------------------------------|--------------------------------------------------------------|
| Open Folder | You want to view the failed directories content           | Open directory modal showing files                           |
| Dismiss     | You do not want to deal with the error                    | Deletes the error                                            |
| Retry       | You have resolved the error, and the import can try again | The error is removed, the directy tries to be imported again |
