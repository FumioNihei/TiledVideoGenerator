$ffmpeg = $Args[0]
$ID = $Args[1]
$blank = $Args[2]
cd ./dest
Write-Host "--------------------"
Write-Host "--- Generate padding videos... -----"
& $ffmpeg -loop 1 -i $blank -vcodec h264 -pix_fmt yuv420p -t 00:00:00.3000000 -r 30 video-0.pad0.mp4
& $ffmpeg -loop 1 -i $blank -vcodec h264 -pix_fmt yuv420p -t 00:00:00.2333334 -r 30 video-1.pad0.mp4
& $ffmpeg -loop 1 -i $blank -vcodec h264 -pix_fmt yuv420p -t 00:00:00.2666667 -r 30 video-1.pad1.mp4
& $ffmpeg -loop 1 -i $blank -vcodec h264 -pix_fmt yuv420p -t 00:00:00.2000000 -r 30 video-2.pad0.mp4
Write-Host "--------------------"
Write-Host "--- Concatenete segmented videos and padding videos... -----"
& $ffmpeg -f concat -safe 0 -i video-0.txt -c copy video-0.mp4
& $ffmpeg -f concat -safe 0 -i video-1.txt -c copy video-1.mp4
& $ffmpeg -f concat -safe 0 -i video-2.txt -c copy video-2.mp4
Write-Host "--------------------"
Write-Host "--- Tiling all videos... -----"
& $ffmpeg `
	-ss 00:00:36.6100000 -i "H:/motivated/media/mixed.wav" `
	-ss 00:00:30.8100000 -i "video-0.mp4" `
	-ss 00:00:17.2433334 -i "video-1.mp4" `
	-ss 00:00:26.3100000 -i "video-2.mp4" `
	-t 00:14:20.4410000 -map 0:a `
	-filter_complex "
		color=s=hd720 [base]
		;[1:v] setpts=PTS-STARTPTS, scale=640x360 [part_0]
		;[2:v] setpts=PTS-STARTPTS, scale=640x360 [part_1]
		;[3:v] setpts=PTS-STARTPTS, scale=640x360 [part_2]
		;[base][part_0] overlay=shortest=1:x=0:y=0 [tmp1]
		;[tmp1][part_1] overlay=shortest=1:x=640:y=0 [tmp2]
		;[tmp2][part_2] overlay=shortest=1:x=0:y=360 
	" `
	-c:v libx264 "${ID}.tile.mp4"
