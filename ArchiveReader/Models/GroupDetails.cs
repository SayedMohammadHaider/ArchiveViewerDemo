using Archive_Reader.Graph;

namespace ArchiveReader.Models
{
    public class GroupDetails
    {
        public List<Members> Members { get; set; }
        public List<GroupFolderDetails> Folders { get; set; }
    }

    public class GroupFolderDetails
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public bool IsParent { get; set; } = false;
    }
}
