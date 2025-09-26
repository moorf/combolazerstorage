![Demo Screenshot](demo.png)
# NOTE: BACKUP EVERYTHING

# requirements:
dotnet 8.0 runtime

# usage:
run .exe (requires .NET 8.0 runtime) from latest release **as admin**
>admin rights are needed for symlink creation

# what it is for
- copying lazer beatmap files to osu!stable filesystem format
- making symlinks from osu!stable filesystem format to use in lazer
- and then using these symlinks to import into osu!lazer database

as a result, you will have a songs folder that is easy to restore in case osu!lazer database gets corrupted. also, you save space by being able to access the same file from both lazer and stable.
moreover, you get a bit more precise control over maps, although a bit limited for now (edited maps will be readded as new, realm will think hash for the file is the same while the file has changed)

## some other use cases:
- migrating database between different schema_versions
- merging multiple databases into one

