using Archive_Reader.Graph;
using ArchiveReader.DbModels;
using System.Security.AccessControl;
using System.Text;

namespace ArchiveReader.Services
{
    public interface ISyncFolderService
    {
        public void SyncFolder();
    }
    public class SyncFolderService : ISyncFolderService
    {
        private readonly GraphApiClient _graphApiClientUI;
        private readonly ArchiveViewerContext _archiveViewerContext;
        private readonly string[] defaultSecurityGroups = new string[] { "BUILTIN\\Administrators", "NT AUTHORITY\\SYSTEM", "CREATOR OWNER", "BUILTIN\\Users", "NT AUTHORITY\\Authenticated Users" };

        public SyncFolderService(GraphApiClient graphApiClientUI, ArchiveViewerContext archiveViewerContext)
        {
            _graphApiClientUI = graphApiClientUI;
            _archiveViewerContext = archiveViewerContext;
        }

        public async void SyncFolder()
        {
            try
            {
                var folders = _archiveViewerContext.Folders.ToList();
                foreach (var folder in folders)
                {
                    try
                    {
                        var groupList = new List<string>();
                        var di = new DirectoryInfo(folder.Path);
                        DirectorySecurity ds = di.GetAccessControl(AccessControlSections.Access);
                        foreach (FileSystemAccessRule fsar in ds.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                        {
                            if (!string.IsNullOrEmpty(fsar.IdentityReference.Value) && !defaultSecurityGroups.Contains(fsar.IdentityReference.Value))
                            {
                                groupList.Add(fsar.IdentityReference.Value.Split('\\').Last());
                            }
                        }
                        StringBuilder filterQuery = new StringBuilder();
                        int count = 0;
                        foreach (var group in groupList)
                        {
                            if (count == 0)
                                filterQuery.Append("displayName eq '" + group + "'");
                            else
                                filterQuery.Append(" or displayName eq '" + group + "'");
                            count++;
                        }
                        var groupFolders = new List<GroupFolder>();
                        if (filterQuery.Length > 0)
                        {
                            var response = await _graphApiClientUI.GetGroup("displayName,id", filterQuery.ToString());
                            for (int i = 0; i < response.Count; i++)
                            {
                                groupFolders.Add(new GroupFolder { Id = Guid.NewGuid(), FolderId = folder.Id, GroupId = response[i].id, GroupName = response[i].displayName });
                            }
                        }
                        var removeGroupFolder = _archiveViewerContext.GroupFolders.Where(x => x.FolderId == folder.Id).ToList();
                        _archiveViewerContext.GroupFolders.RemoveRange(removeGroupFolder);
                        _archiveViewerContext.GroupFolders.AddRange(groupFolders);
                        _archiveViewerContext.SaveChanges();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
