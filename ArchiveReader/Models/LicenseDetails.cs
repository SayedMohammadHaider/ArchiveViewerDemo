namespace ArchiveReader.Models
{
    public class LicenseDetails
    {
        public string licenseId { get; set; }
        public string companyName { get; set; }
        public int maxUserCapacity { get; set; }
        public string expiryDate { get; set; }
        public List<User> users { get; set; }

    }
}
