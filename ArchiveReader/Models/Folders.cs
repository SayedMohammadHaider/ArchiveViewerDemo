namespace ArchiveReader.Models
{
    public class Folders
    {
        public string database { get; set; }
        public string title { get; set; }
        public string path { get; set; }
        public bool isAzure { get; set; }
        public string tenantId { get; set; }
        public string clientId { get; set; }
        public string blobContainerName { get; set; }
    }
}
