
$ffmpeg = "D:\DeskTop\ffmpeg-4.2.2-win64-static\ffmpeg-4.2.2-win64-static\bin\ffmpeg.exe"
$timeBase = 30
$destDir = "H:\motivated\media\export-tmp"
$targetList = "./targets.txt"
$blank = Convert-Path ".\blank.png"


$targets = ( Get-Content $targetList | ConvertFrom-Csv -Delimiter "`t" )

foreach ($target in $targets) {

    $ID = $target.ID
    $xml = $target.XmlPath
    $start = $target.Start
    $end = $target.End

    $dest = [IO.Path]::Combine( $destDir, [IO.Path]::GetFileNameWithoutExtension($ID) )

    Write-Host "$xml -> $dest"
    New-Item $dest -ItemType Directory

    dotnet run $xml $start $end $timeBase $dest


    & powershell (Join-Path $dest Tiling.ps1) $ffmpeg $ID $blank
    & powershell (Join-Path $dest SyncAndTrim.ps1) $ffmpeg $ID
}
