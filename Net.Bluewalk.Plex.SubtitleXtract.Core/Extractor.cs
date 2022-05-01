using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Net.Bluewalk.Plex.SubtitleXtract.Core.Models;
using Serilog;
using Stream = Net.Bluewalk.Plex.SubtitleXtract.Core.Models.Stream;

namespace Net.Bluewalk.Plex.SubtitleXtract.Core
{
    public class Extractor : IDisposable
    {
        public static string Database = "com.plexapp.plugins.library.db";
        public static string BlobDatabase = "com.plexapp.plugins.library.blobs.db";

        private readonly SqliteConnection _databaseConnection;
        private readonly SqliteConnection _blobDatabaseConnection;
        private readonly ILogger _logger;

        public Extractor()
        {
            _logger = Log.ForContext<Extractor>();

            var path = Environment.GetEnvironmentVariable("PLEX_HOME") ?? Environment.CurrentDirectory;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var reg = Registry.CurrentUser.OpenSubKey(@"Software\Plex, Inc.\Plex Media Server", false);
                var location = reg?.GetValue("LocalAppDataPath")?.ToString();

                if (!string.IsNullOrEmpty(location))
                    path = location;
            }

            path = Path.Combine(path, @"Plex Media Server\Plug-in Support\Databases");

            _logger.Information("Plex data path: {path}", path);

            _logger.Information("Opening databases");
            _databaseConnection = new SqliteConnection($"Data Source={Path.Combine(path, Database)}");
            _databaseConnection.Open();
            _blobDatabaseConnection = new SqliteConnection($"Data Source={Path.Combine(path, BlobDatabase)}");
            _blobDatabaseConnection.Open();

            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public IEnumerable<SubtitleStream> GetSubtitles()
        {
            return _blobDatabaseConnection.Query<SubtitleStream>("SELECT linked_id, blob FROM blobs WHERE blob_type = 3");
        }

        public IEnumerable<Stream> GetStreams()
        {
            return _databaseConnection.Query<Stream>(
                "SELECT stream.id, parts.file, stream.codec, stream.language, stream.forced FROM media_streams AS stream " +
                    "INNER JOIN media_parts AS parts ON parts.id=stream.media_part_id");
        }

        public void Run()
        {
            var subtitles = GetSubtitles();
            _logger.Information("Fetched {count} subtitle streams", subtitles.Count());

            var streams = GetStreams();
            _logger.Information("Fetched {count} streams", streams.Count());

            foreach (var subtitle in subtitles)
            {
                var stream = streams?.FirstOrDefault(q => q.Id == subtitle.LinkedId);
                if (stream == null) continue;

                var filename = Path.GetFileNameWithoutExtension(stream.File);
                filename += $".{stream.Language}";

                if (stream.Forced) filename += ".forced";

                filename += $".{stream.Codec}";

                _logger.Information("Extracting {id}: {filename}", subtitle.LinkedId, filename);

                var target = Path.Combine(Path.GetDirectoryName(stream.File) ?? Environment.CurrentDirectory, filename);

                if (!File.Exists(target))
                {
                    File.WriteAllText(target, subtitle.Srt);
                    _logger.Information("Subtitles written to {target}", target);
                }
                else
                    _logger.Warning("Couldn't save subtitle, file {target} already exists", target);

                _logger.Information("Removing subtitle from database");

                _blobDatabaseConnection.Execute("DELETE FROM blobs WHERE linked_id = @linkedId",
                    new { linkedId = subtitle.LinkedId });
                _databaseConnection.Execute(
                    "UPDATE media_part_settings SET selected_subtitle_stream_id = NULL WHERE selected_subtitle_stream_id = @linkedId",
                    new { linkedId = subtitle.LinkedId });
                _databaseConnection.Execute("DELETE FROM media_stream_settings WHERE media_stream_id = @linkedId",
                    new { linkedId = subtitle.LinkedId });
                _databaseConnection.Execute("DELETE FROM media_streams WHERE id = @linkedId",
                    new { linkedId = subtitle.LinkedId });

                _logger.Information("Subtitle removed from database");
            }
        }
        public void Dispose()
        {
            _logger.Information("Closing databases");

            _databaseConnection.Dispose();
            _blobDatabaseConnection.Dispose();
        }
    }
}