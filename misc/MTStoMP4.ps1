

$ffmpeg = "D:\DeskTop\ffmpeg-4.2.2-win64-static\ffmpeg-4.2.2-win64-static\bin\ffmpeg.exe"


$srcs = Get-ChildItem "H:\motivated\media\original\overview"
$destDir = "H:\motivated\media\overview"

foreach ( $src in $srcs ) {

    $dest = ( Join-Path $destDir $src.BaseName )
    Write-Host $src.Name "->" $dest


    # # MTS to mp4. fast.
    # & $ffmpeg -i $src.FullName -vcodec copy -acodec copy $dest

    # # MTS to mp4, 5.1ch to 1ch, normalize audio.
    # & $ffmpeg -i $src.FullName -vcodec copy  -ac 1 -filter:a loudnorm "$dest.mp4"

    # MTS to mp4, remove head, framerate=30, monaural.
    & $ffmpeg -ss 00:00:1.000 -i $src.FullName -r 30 -ac 1 "$dest.mp4"

}