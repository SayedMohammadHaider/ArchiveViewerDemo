namespace ArchiveReader.Models
{
    public class User
    {
        public string userName { get; set; }
        public string active { get; set; }
        public List<Folders> folders { get; set; }
    }
}
