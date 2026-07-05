# Sync Device Folders

A utility for syncing directories on different devices.
It is mainly intended for syncing any directories on various devices (Windows and Linux are supported so far). The utility creates a synchronization folder for each device and makes a backup of the synchronized data before synchronization.

Using: 
- download the latest release: https://github.com/Subreal-Team/SubReal.SyncDeviceFolders/releases
- copy the two files from the archive to a separate directory on the flash drive or on the device folder
- set up the synchronization configuration (example): https://github.com/Subreal-Team/SubReal.SyncDeviceFolders/blob/main/config.json.sample
- run the appropriate program file for a specific system

Example settings for sync **fheroes2** game saves on three different devices (Windows, Steam Deck and Linux):

```
{
    "FolderSettings": {
        "fheroes2": {
            "DESKTOP": {
                "Path": "C:\\Users\\username\\AppData\\Roaming\\fheroes2\\files\\save",
                "Process": "fheroes2",
                "Backups": 3,
                "Filter": "*.*"
            },
            "steamdeck": {
                "Path": "/home/deck/.var/app/io.github.ihhub.Fheroes2/data/fheroes2/files/save/",
                "Process": "fheroes2",
                "Backups": 3,
                "Filter": "*.*"
            },
            "cachyos": {
                "Path": "/home/username/.var/app/io.github.ihhub.Fheroes2/data/fheroes2/files/save/",
                "Process": "fheroes2",
                "Backups": 3,
                "Filter": "*.*"
            }
        }
    }
}
```
