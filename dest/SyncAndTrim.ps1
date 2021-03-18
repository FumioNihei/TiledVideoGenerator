$ffmpeg = $Args[0]
$ID = $Args[1]
cd ./dest
& $ffmpeg -ss 00:00:36.6100000 -i H:/motivated/media/mixed.wav -t 00:14:20.4410000 "${ID}.mixed.wav"
& $ffmpeg -ss 00:00:36.6100000 -i H:/motivated/media/counsellor.wav -t 00:14:20.4410000 "${ID}.counsellor.wav"
& $ffmpeg -ss 00:00:36.6100000 -i H:/motivated/media/patient.wav -t 00:14:20.4410000 "${ID}.patient.wav"
& $ffmpeg -ss 00:00:30.8100000 -i video-0.mp4 -t 00:14:20.4410000 -vf framerate=30 "${ID}.video-0.mp4"
& $ffmpeg -ss 00:00:17.2433334 -i video-1.mp4 -t 00:14:20.4410000 -vf framerate=30 "${ID}.video-1.mp4"
& $ffmpeg -ss 00:00:26.3100000 -i video-2.mp4 -t 00:14:20.4410000 -vf framerate=30 "${ID}.video-2.mp4"
