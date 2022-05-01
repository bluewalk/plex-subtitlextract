using System.IO.Compression;
using System.Text;

namespace Net.Bluewalk.Plex.SubtitleXtract.Core.Models
{
    public class SubtitleStream
    {
        public long LinkedId { get; set; }
        public byte[] Blob { get; set; }
        public string Srt { get
            {
                using (var compressedStream = new MemoryStream(Blob))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    return Encoding.UTF8.GetString(resultStream.ToArray());
                }
            }
        }
    }
}
