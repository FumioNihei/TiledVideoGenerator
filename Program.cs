using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace TiledVideoGenerator
{
    class TrackInfo {

		public enum MediaType { Audio = 0, Video = 1 }

		public MediaType Type { get; private set; }

		public string Name { get; private set; }

		public string RepresentMediaPath { get; private set; }
		public MediaInfo[] MediaInformations { get; private set; }

		public TrackInfo( string name, IEnumerable<MediaInfo> mediaInformations, MediaType type ) {
			this.Name = name;
			this.MediaInformations = mediaInformations
				.OrderBy( mediaInfo => mediaInfo.StartTimeInFrame )
				.ToArray();

			this.RepresentMediaPath = this.MediaInformations.Length == 1
				? this.MediaInformations.First().FilePath
				: $"{this.Name}.mp4";

			this.Type = type;
		}

	}

	class MediaInfo {

		public enum MediaType { Original, Padding }

		public MediaType Type { get; private set; }

		public double TimeBase { get; set; }

		public string FilePath { get; private set; }
		public long StartTimeInFrame { get; private set; }
		public long EndTimeInFrame { get; private set; }

		public TimeSpan StartTimeInTimeCode { get; private set; }
		public TimeSpan EndTimeInTimeCode { get; private set; }

		public TimeSpan Duration { get => this.EndTimeInTimeCode - this.StartTimeInTimeCode; }

		public MediaInfo( string filePath, long startFrame, long endFrame, MediaType type, double timeBase ) {

			this.FilePath = filePath;
			this.Type = type;
			this.TimeBase = timeBase;
			//this.TimeBase = 30;

			this.StartTimeInFrame = startFrame;
			this.EndTimeInFrame = endFrame;

			this.StartTimeInTimeCode = TimeSpan.FromSeconds( StartTimeInFrame / this.TimeBase );
			this.EndTimeInTimeCode = TimeSpan.FromSeconds( EndTimeInFrame / this.TimeBase );
		}

		public MediaInfo( string filePath, TimeSpan start, TimeSpan end, MediaType type ) {

			this.FilePath = filePath;

			this.StartTimeInFrame = 0;
			this.EndTimeInFrame = 0;

			this.StartTimeInTimeCode = start;
			this.EndTimeInTimeCode = end;

			this.Type = type;
		}

		public override string ToString() {
			return $"{FilePath}\t{TimeBase}\t{StartTimeInTimeCode}\t{EndTimeInTimeCode}\t{StartTimeInTimeCode.TotalSeconds}\t{EndTimeInTimeCode.TotalSeconds}";
		}

	}




	class Program {

		//public static double TimeBase { get; set; } = 30.0;


		private static IEnumerable<TrackInfo> ParseTracks( XmlDocument document, TrackInfo.MediaType type ) {

			var media = type == TrackInfo.MediaType.Audio ? "audio"
				: type == TrackInfo.MediaType.Video ? "video"
				: null;

			var tracks = document.SelectNodes( $"xmeml/project/children/sequence/media/{media}/track" );

			return Enumerable.Range( 0, tracks.Count ).Select( idx => {

				XmlNode track = tracks[idx];
				var trackID = $"{media}-{idx}";

				var clips = track.SelectNodes( @"clipitem" );

				var infos = Enumerable.Range( 0, clips.Count ).Select( i => {

					var clip = clips[i];

					var info = new MediaInfo(
						filePath: clip.SelectSingleNode( @"file/pathurl" ).InnerText.Replace( "file://localhost/", "" ),
						startFrame: long.Parse( clip.SelectSingleNode( @"start" ).InnerText ),
						endFrame: long.Parse( clip.SelectSingleNode( @"end" ).InnerText ),
						type: MediaInfo.MediaType.Original,
						timeBase: double.Parse( clip.SelectSingleNode( @"file/rate/timebase" ).InnerText )
					);

					return info;

				} ).ToArray();

				return new TrackInfo(
					name: trackID,
					mediaInformations: infos,
					type: type
				);
			} );
		}





		private class Point {
			public int X { get; set; }
			public int Y { get; set; }

		}


		private static IEnumerable<string> FilterOptions( int screenWidth, int screenHeight, IEnumerable<TrackInfo> tracks ) {

			var setupScreen = $"color=s=hd{screenHeight} [base]";


			var c = tracks.Count() <= 4 ? 2 : tracks.Count() <= 9 ? 3 : 4;

			var w = screenWidth / c; // video width
			var h = screenHeight / c; // video width


			//var positions = Enumerable.Range( 0, c ).SelectMany( x =>
			//	Enumerable.Range( 0, c ).Select( y => new Point() {
			//		X = (int)( screenWidth * x / (float)c ),
			//		Y = (int)( screenHeight * y / (float)c )
			//	} )
			//).ToArray();

			var positions = Enumerable.Range( 0, c ).SelectMany( y =>
				Enumerable.Range( 0, c ).Select( x => new Point() {
					X = (int)( screenWidth * x / (float)c ),
					Y = (int)( screenHeight * y / (float)c )
				} )
			).ToArray();


			//foreach( var pos in positions ) {
			//	Console.WriteLine( $"{pos.X}, {pos.Y}" );
			//}


			string PartName( int idx ) => $"part_{idx}";

			var parts = tracks.Select( ( track, idx ) => $"[{idx+1}:v] setpts=PTS-STARTPTS, scale={w}x{h} [{PartName( idx )}]" );

			var tiles = tracks.Select( ( track, idx ) => {

				var from = idx == 0 ? "[base]" : $"[tmp{idx}]";
				var to = idx == tracks.Count() - 1 ? "" : $"[tmp{idx + 1}]";

				var x = positions[idx].X;
				var y = positions[idx].Y;

				return $"{from}[{PartName( idx )}] overlay=shortest=1:x={x}:y={y} {to}";
			} );


			//var filterOptions = string.Join( "; ", new string[] { setupScreen }.Concat( parts ).Concat( tiles ) );

			//return filterOptions;

			string AddSeparator( string line ) => $";{line}";

			//var filterOptions = new string[] { setupScreen }.Concat( parts ).Concat( tiles );
			var filterOptions = new string[] { setupScreen }.Concat( parts.Select( AddSeparator ) ).Concat( tiles.Select( AddSeparator ) );

			return filterOptions;
		}


		private static IEnumerable<string> DecideBoundingBox( IEnumerable<TrackInfo> tracks, string dest ) {

			var screenWidth = 1280;
			var screenHeight = 720;

			var audioTrack = tracks.First( track => track.Type == TrackInfo.MediaType.Audio );

			var duration = audioTrack.MediaInformations[0].Duration;

			//var ffmpegExecution = $"& $ffmpeg {string.Join( " ", tracks.Select( track => $"-ss {track.MediaInformations[0].StartTimeInTimeCode} -i {track.RepresentMediaPath}" ) )}";


			var firstInputAudio = tracks
				.OrderBy( track => int.Parse( track.Name.Split( '-' )[1] ) )
				.First( track => track.Type == TrackInfo.MediaType.Audio );


			var inputs = tracks
				.Where( track => track.Type == TrackInfo.MediaType.Video || track.Name == firstInputAudio.Name )
				.Select( track => $@"-ss {track.MediaInformations[0].StartTimeInTimeCode} -i ""{track.RepresentMediaPath}""" );

			var filterOptions = FilterOptions( screenWidth, screenHeight, tracks.Where( track => track.Type == TrackInfo.MediaType.Video ) );

			var script = new[] { $"& $ffmpeg `" }
				.Concat( inputs.Select( input => $"\t{input} `" ) )
				.Concat( new[] {
					$"\t-t {duration} -map 0:a `",
					$"\t-filter_complex \"" } )
				.Concat( filterOptions.Select( opt => $"\t\t{opt}" ) )
				.Concat( new[] {
					$"\t\" `",
					$"\t-c:v libx264 {dest}"
				} );

			//foreach( var line in script ) {
			//	Console.WriteLine( line );
			//}

			return script;

		}






		private static IEnumerable<string> FilterOptionsForMatricsCorpus( int screenWidth, int screenHeight, IEnumerable<TrackInfo> tracks ) {

			var setupScreen = $"color=c=black:s={screenWidth}x{screenHeight} [base]";

			var scale = "512x288";
			var crop = "460:288:26:0";
			var font = @"C\\:/Windows/Fonts/segoeui.ttf";


			var fontSize = 30;
			var fontColor = "black";

			var options = new [] {
				$"[1:v] setpts=PTS-STARTPTS, scale={scale}, crop={crop}, drawtext=fontfile={font}:text='A':fontsize={fontSize}:fontcolor={fontColor}:x=18:y=18: [part_0]",
				$"[2:v] setpts=PTS-STARTPTS, scale={scale}, crop={crop}, drawtext=fontfile={font}:text='B':fontsize={fontSize}:fontcolor={fontColor}:x=422:y=18: [part_1]",
				$"[3:v] setpts=PTS-STARTPTS, scale={scale}, crop={crop}, hflip, drawtext=fontfile={font}:text='C':fontsize={fontSize}:fontcolor={fontColor}:x=422:y=18: [part_2]",
				$"[4:v] setpts=PTS-STARTPTS, scale={scale}, crop={crop}, hflip, drawtext=fontfile={font}:text='D':fontsize={fontSize}:fontcolor={fontColor}:x=18:y=18: [part_3]",
				$"[5:v] setpts=PTS-STARTPTS, scale=256x144, crop=230:144:13:0 [part_4]",
				$"[base] [part_0] overlay=shortest=1:x=0:y=0 [tmp1]",
				$"[tmp1] [part_1] overlay=shortest=1:x=564:y=0 [tmp2]",
				$"[tmp2] [part_2] overlay=shortest=1:x=564:y=288 [tmp3]",
				$"[tmp3] [part_3] overlay=shortest=1:x=0:y=288 [tmp4]",
				$"[tmp4] [part_4] overlay=shortest=1:x=397:y=216"
			};


			string AddSeparator( string line ) => $";{line}";

			var filterOptions = new string[] { setupScreen }.Concat( options.Select( AddSeparator ) );

			return filterOptions;
		}

		private static IEnumerable<string> DecideBoundingBoxForMatricsCorpus( IEnumerable<TrackInfo> tracks, string dest ) {

			var screenWidth = 1024;
			var screenHeight = 576;

			var audioTrack = tracks.First( track => track.Type == TrackInfo.MediaType.Audio );

			var duration = audioTrack.MediaInformations[0].Duration;


			var inputs = tracks.Select( track => $@"-ss {track.MediaInformations[0].StartTimeInTimeCode} -i ""{track.RepresentMediaPath}""" );

			var filterOptions = FilterOptionsForMatricsCorpus( screenWidth, screenHeight, tracks.Where( track => track.Type == TrackInfo.MediaType.Video ) );

			var script = new[] { $"& $ffmpeg `" }
				.Concat( inputs.Select( input => $"\t{input} `" ) )
				.Concat( new[] {
					$"\t-t {duration} -map 0:a `",
					$"\t-filter_complex \"" } )
				.Concat( filterOptions.Select( opt => $"\t\t{opt}" ) )
				.Concat( new[] {
					$"\t\" `",
					$"\t-c:v libx264 {dest}"
				} );

			//foreach( var line in script ) {
			//	Console.WriteLine( line );
			//}

			return script;

		}






		static void Main( string[] args ) {

			// Execute from Run.ps1
			var MediaSyncXmlPath = args[0];
			var SessionStartTime = TimeSpan.Parse( args[1] );
			var SessionEndTime = TimeSpan.Parse( args[2] );
			var timeBase = args[3];
			var dest = args[4];

			//var times = args[1];
			//var session = args[3];



			//var timesAt = File.ReadAllLines( times ).Skip( 1 ).ToDictionary(
			//	line => line.Split( '\t' )[0],
			//	line => {
			//		var Start = TimeSpan.Parse( line.Split( '\t' )[1] );
			//		var end = TimeSpan.Parse( line.Split( '\t' )[2] );
			//		var Duration = end - Start;

			//		return new { Start, Duration };
			//	}
			//);




			var document = new XmlDocument();
			document.Load( MediaSyncXmlPath );



			var audioTracks = ParseTracks( document, TrackInfo.MediaType.Audio );
			var videoTracks = ParseTracks( document, TrackInfo.MediaType.Video );


			var mediaTracks = audioTracks.Where( audioTrack => {
			
				var videoFiles = new HashSet<string>( videoTracks.SelectMany( track => track.MediaInformations.Select( mediaInfo => mediaInfo.FilePath ) ) );

				return !audioTrack.MediaInformations.Any( mediaInfo => videoFiles.Contains( mediaInfo.FilePath ) );

			} ).Concat( videoTracks );


			mediaTracks = mediaTracks
				.OrderBy( track => (int)track.Type )
				.ThenBy( track => int.Parse( track.Name.Split( '-' )[1] ) );






			var paddedMediaTracks = mediaTracks.Select( track => {

				if( track.MediaInformations.Length == 1 ) {
					return track;
				}

				var infos = track.MediaInformations;
				var trackID = track.Name;

				var paddingMediaInfos = infos.Zip( infos.Skip( 1 ), ( Current, Next ) => new { Current, Next } ).Select( ( pair, idx ) => new MediaInfo(
					filePath: $"{trackID}.pad{idx}.mp4",
					startFrame: pair.Current.EndTimeInFrame,
					endFrame: pair.Next.StartTimeInFrame,
					type: MediaInfo.MediaType.Padding,
					//timeBase: 30
					timeBase: double.Parse( timeBase )
				) );

				return new TrackInfo(
					name: trackID,
					mediaInformations: infos.Concat( paddingMediaInfos ).OrderBy( mediaInfo => mediaInfo.StartTimeInFrame ),
					type: track.Type
				);

			} );

			File.WriteAllLines(
				Path.Combine( dest, $"MediaInfos.txt" ),
				paddedMediaTracks.SelectMany( track => track.MediaInformations.Select( mediaInfo => string.Join( "\t", new string[] {
					track.Name,
					mediaInfo.ToString()
				} ) ) )
			);












			//var sessionStart = timesAt[session].Start;
			//var sessionDuration = timesAt[session].Duration;

			var sessionStart = SessionStartTime;
			var sessionDuration = SessionEndTime - SessionStartTime;


			var audio = paddedMediaTracks.First( track => track.Type == TrackInfo.MediaType.Audio );
			var audioStart = audio.MediaInformations[0].StartTimeInTimeCode;



			var adjustedMediaTracks = paddedMediaTracks.Select( track => {

				var mediaInfos = track.MediaInformations.Select( media => {

					var start = sessionStart + ( audioStart - track.MediaInformations[0].StartTimeInTimeCode );

					return new MediaInfo(
						filePath: media.FilePath,
						start: start,
						end: start + sessionDuration,
						type: media.Type
					);
				} );

				return new TrackInfo( name: track.Name, mediaInformations: mediaInfos, type: track.Type );
			} );






			var replacedExtentionAt = new Dictionary<string, string>() {
				{ ".MTS", ".mp4" },
				{ ".wmv", ".mp4" },
			};
			string ReplaceExtension( string extension ) => replacedExtentionAt.ContainsKey( extension ) ? replacedExtentionAt[extension] : extension;


			var __SyncAndTrim = adjustedMediaTracks.Select( track => {

				var start = track.MediaInformations[0].StartTimeInTimeCode;

				//var extension = Path.GetExtension( track.RepresentMediaPath );
				//extension = replacedExtentionAt.ContainsKey( extension )
				//	? replacedExtentionAt[Path.GetExtension( track.RepresentMediaPath )]
				//	: extension;

				var extension = ReplaceExtension( Path.GetExtension( track.RepresentMediaPath ) );
				//var outFilePath = Path.Combine( dest, $"{Path.GetFileNameWithoutExtension( track.RepresentMediaPath )}{extension}" );
				var outFilePath = $"\"${{ID}}.{Path.GetFileNameWithoutExtension( track.RepresentMediaPath )}{extension}\"";


				return $"& $ffmpeg -ss {start} -i {track.RepresentMediaPath} -t {sessionDuration}{(track.Type == TrackInfo.MediaType.Video ? " -vf framerate=30 " : " ")}{outFilePath}";
			} );

			var SyncAndTrim = new[] { $@"$ffmpeg = $Args[0]", $@"$ID = $Args[1]", $"cd {dest}" }
				.Concat( __SyncAndTrim );

			File.WriteAllLines( Path.Combine( dest, $"SyncAndTrim.ps1" ), SyncAndTrim );




			//var individuals = Directory.GetFiles( @"H:\MATRICS-Corpus\__private\RAW\Audio-Individual" );

			//var __SyncAndTrimForIndividualAudio = adjustedMediaTracks
			//	.Where( track => track.Type == TrackInfo.MediaType.Audio )
			//	.SelectMany( track => {

			//		var start = track.MediaInformations[0].StartTimeInTimeCode;

			//		var fileName = Path.GetFileNameWithoutExtension( track.RepresentMediaPath );
			//		var extension = Path.GetExtension( track.RepresentMediaPath );


			//		return individuals
			//			.Where( individual => Path.GetFileNameWithoutExtension( individual ).StartsWith( fileName ) )
			//			.Select( individual => {

			//				var outFilePath = Path.Combine( dest, $"{Path.GetFileNameWithoutExtension( individual )}{extension}" );

			//				return $"& $ffmpeg -ss {start} -i {individual} -t {sessionDuration} {outFilePath}";
			//			} );
			//	}
			//);

			//var SyncAndTrimForIndividualAudio = new[] { $@"$ffmpeg = $Args[0]" }
			//	.Concat( __SyncAndTrimForIndividualAudio );

			//File.WriteAllLines( Path.Combine( dest, $"SyncAndTrimForIndividualAudio.ps1" ), SyncAndTrimForIndividualAudio );










			var padding = paddedMediaTracks.SelectMany( track => track.MediaInformations
				.Where( mediaInfo => mediaInfo.Type == MediaInfo.MediaType.Padding )
				//.Select( mediaInfo => $@"& $ffmpeg -loop 1 -i pen.jpg -vcodec h264 -pix_fmt yuv420p -t {mediaInfo.EndTimeInTimeCode - mediaInfo.StartTimeInTimeCode} -r 30 {mediaInfo.FilePath}" )
				.Select( mediaInfo => $@"& $ffmpeg -loop 1 -i $blank -vcodec h264 -pix_fmt yuv420p -t {mediaInfo.EndTimeInTimeCode - mediaInfo.StartTimeInTimeCode} -r {timeBase} {mediaInfo.FilePath}" )
			);

			var paddingTargetTracks = paddedMediaTracks.Where( track => track.MediaInformations.Length > 1 );

			foreach( var track in paddingTargetTracks ) {
				File.WriteAllLines(
					Path.Combine( dest, $"{track.Name}.txt" ),
					track.MediaInformations.Select( mediaInfo => $"file {mediaInfo.FilePath}" )
				);
			}

			var concatenations = paddingTargetTracks.Select( track => $"& $ffmpeg -f concat -safe 0 -i {track.Name}.txt -c copy {track.RepresentMediaPath}" );


			//var tiled = DecideBoundingBox( adjustedMediaTracks, Path.Combine( dest, $"tile.mp4" ) );
			var tiled = DecideBoundingBox( adjustedMediaTracks, @"""${ID}.tile.mp4""" );
			var forAnnotation = DecideBoundingBoxForMatricsCorpus( adjustedMediaTracks, Path.Combine( dest, $"$ID.mp4" ) );



			string Dash( int repeat ) => string.Join( "", Enumerable.Repeat( "-", repeat ) );
			IEnumerable<string> Message( string message ) => new[] { $@"Write-Host ""{Dash( 20 )}""", $@"Write-Host ""{Dash(3)} {message} {Dash(5)}""" };


			var Tiling = new[] { $@"$ffmpeg = $Args[0]", $@"$ID = $Args[1]", $@"$blank = $Args[2]", $"cd {dest}" }
				.Concat( Message( "Generate padding videos..." ) )
				.Concat( padding )
				.Concat( Message( "Concatenete segmented videos and padding videos..." ) )
				.Concat( concatenations )
				.Concat( Message( "Tiling all videos..." ) )
				.Concat( tiled )
				;
				//.Concat( Message( "Tiling all videos to Generate Annotation Video..." ) )
				//.Concat( forAnnotation );

			File.WriteAllLines( Path.Combine( dest, $"Tiling.ps1" ), Tiling );




		}

		
	}
}
