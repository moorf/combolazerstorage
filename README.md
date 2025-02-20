![Demo Screenshot](demo.png)

# requirements:
python with tkinter, dotnet 8.0 runtime

# what it is for
- copying lazer beatmap files to osu!stable filesystem format (operation mode 1)
- making symlinks from osu!stable filesystem format to use in lazer (operation mode 2)
- then using these symlinks to import into osu!lazer database (operation mode 3)

as a result, you will have a songs folder that is easy to restore in case osu!lazer database gets corrupted. also, you save space by being able to access the same file from both lazer and stable.
moreover, you get a bit more precise control over maps, although a bit limited for now (hashes are written to realm from songs folder names for now)

## some other use cases:
- migrating database between different schema_versions
- merging multiple databases into one
# usage:
launch main.py for gui or look up source code
example usage without gui (merging two databases together):
```
combolazerstorage.exe 1 D:\osul D:\osulazer_files C:\Users\%username%\Desktop\23\client_46.realm 46
combolazerstorage.exe 1 D:\osul D:\osulazer_files C:\Users\%username%\Desktop\linux\client_47.realm 47
combolazerstorage.exe 2 D:\osul C:\Users\%username%\AppData\Roaming\osu-development\files 46
combolazerstorage.exe 3 D:\osul C:\Users\%username%\AppData\Roaming\osu-development\files C:\Users\%username%\AppData\Roaming\osu-development\client_46.realm 46
```
# admin rights are needed for symlink creation
