# Sync Device Folders

A utility for syncing directories on different devices.
It is mainly intended for syncing any directories on various devices (Windows and Linux are supported so far). The utility creates a synchronization folder for each device and makes a backup of the synchronized data before synchronization.

## Using: 
- download the latest release: https://github.com/Subreal-Team/SubReal.SyncDeviceFolders/releases
- copy the two files from the archive to a separate directory on the flash drive or on the device folder
- set up the synchronization configuration (example): https://github.com/Subreal-Team/SubReal.SyncDeviceFolders/blob/main/config.json.sample
- run the appropriate program file for a specific system

## Recommendations for running on systems:
- Linux based: run ``SyncDeviceFolders.linux-x64`` (or arm64 if you have an ARM processor) from the terminal or right-click in the drop-down menu ``run in konsole`` for example
- Mac OS based: run ``SyncDeviceFolders.osx-x64`` either (or arm64 if you have an ARM processor). 
Example of launching from terminal: ``./SyncDeviceFolders.osx-arm64`` for Mac Book Air on M1 processor (ARM based).

## Build on your system:
- download the code from this repository
- run the build and publish command in project directory (``win-x64``, ``linux-x64``, ``osx-arm64`` - platforms for which you want to get the executable file):
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Settings example 
For sync **fheroes2** game saves on four different devices (Windows, Steam Deck, Linux and Mac OS):

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
            },
            "MacBook-Air": {
                "Path": "/Users/username/Library/Application Support/fheroes2/files/save/",
                "Process": "fheroes2"
            }
        }
    }
}
```
