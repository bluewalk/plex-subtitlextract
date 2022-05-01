namespace Net.Bluewalk.Plex.SubtitleXtract.Core.Models
{
    public class Stream
    {
        public long Id { get; set; }
        public string File { get; set; }
        public string Codec { get; set; }
        public string Language { get; set; }
        public bool Forced { get; set; }
    }
}
