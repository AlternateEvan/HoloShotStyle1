<#
.SYNOPSIS
Prepares your library platform cache.

.DESCRIPTION
Creates the cache if it doesn't exist, moves your current library folder into the cache,
and turns the Library directory into a junction pointing at the cached folder.
#>
param (
    [string]$ProjectDir = (gi $PSScriptRoot).parent.fullname,
    [string]$Platform = "StandaloneWindows"
)

$Library = "$ProjectDir\\Library"
$CacheRoot = "$ProjectDir\\LibraryCache"
$CacheDir = "$CacheRoot\\$Platform"

ni $CacheRoot -type directory -force | out-null

<# Whether path is a junction. Thanks to http://stackoverflow.com/a/818054 #>
function Test-ReparsePoint([string]$path) {
  $file = gi $path -force -ea 0
  return [bool]($file.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

if (Test-ReparsePoint $Library) {
    write "$Library is already a junction; nothing to do."
    exit 0;
}

if (Test-Path $CacheDir) {
    rm -r $CacheDir
}

if (Test-Path $Library) {
    mv $Library $CacheDir
} else {
    ni $CacheDir -type directory -force | out-null
}

cmd /c mklink /j $Library $CacheDir
