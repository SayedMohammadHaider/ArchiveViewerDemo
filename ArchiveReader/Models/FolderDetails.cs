using Archive_Reader.Graph;

namespace ArchiveReader.Models
{
    public class FolderDetails
    {
        public List<Archive_Reader.Graph.GroupDetails> groupDetails { get; set; }
        public List<Members> Members { get; set; }
    }
}
