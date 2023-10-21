using Archive_Reader.Graph;
using Archive_Reader.Models;
using ArchiveReader;
using ArchiveReader.DbModels;
using ArchiveReader.Models;
using ArchiveReader.Services;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data.OleDb;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive_Reader.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraphApiClient _graphApiClientUI;
        private readonly IConfiguration _configuration;
        public List<string> _tempFolders = new List<string>();
        public string tempFolder = String.Empty;
        private readonly ArchiveViewerContext _archiveViewerContext;
        private readonly string[] defaultSecurityGroups = new string[] { "BUILTIN\\Administrators", "NT AUTHORITY\\SYSTEM", "CREATOR OWNER", "BUILTIN\\Users", "NT AUTHORITY\\Authenticated Users" };
        private static bool timerEnabled = false;
        private readonly string _appVersion = "V 3.0.11";

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, GraphApiClient graphApiClientUI, ArchiveViewerContext archiveViewerContext)
        {
            _graphApiClientUI = graphApiClientUI;
            _logger = logger;
            _configuration = configuration;
            _archiveViewerContext = archiveViewerContext;
            _appVersion = "V " + _configuration.GetValue<string>("Version");
        }

        private string GenerateKey()
        {
            string input = "Adopteq AB Borås";
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(input));
                Guid result = new Guid(hash);
                return result.ToString();
            }
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.expiryDate = "30-11-2023";
            ViewBag.validToTextColor = "greenColor";
            ViewBag.validToTooltipBackgroundColor = "validToTooltipGreen";
            ViewBag.tooltipText = "";
            ViewBag.isAdmin = false;
            ViewBag.appVersion = _appVersion;
            return View();
        }

        public async Task<bool> IsLoggedInUserAdmin()
        {
            var adminGroupDetails = _archiveViewerContext.AdminDetails.Select(x => x.AdminGroupIds).ToList();
            foreach (var adminGroup in adminGroupDetails)
            {
                var loggedInUserGroups = await _graphApiClientUI.GetLoggedInUserGroupList();
                if (adminGroup != null && adminGroup.Split(",").Any(x => loggedInUserGroups.Contains(x)))
                {
                    return true;
                }
            }
            return false;
        }

        [HttpGet]
        public string GetHtmlFromEmlFilePath(string filePath, string fileName)
        {  
            var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\email.htm");
            return fileData;
        }

        [HttpGet]
        public async Task<string> LoadFolders()
        {
            var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\LoadFolders.json");
            return fileData;
        }

        [HttpGet]
        public async Task<bool> folderPermission(string folderId)
        {
            return true;
        }

        [HttpGet]
        public string ReadTitleJsonFile([FromQuery] string folderPath)
        {
            try
            {
                var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\ReadTitleJsonFIle.json");
                var fileList = JsonConvert.DeserializeObject<List<Files>>(fileData);
                return JsonConvert.SerializeObject(fileList);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        public string SearchRelatedDocument(string searchTerm, string folderPath)
        {
            return "72564FAA249F64D60025875800625178,FF00B4485A52ED7A002586870043AB54,3771B6E5525D01B200258685005B0951,62A873433324E3C2C1258527003F6490,3575DFA12CC95DB1C1258527002BBFA8,C1257B18003831A0E16C8F03408C29F9";
        }

        [HttpGet]
        public async Task<string> searchDocuments(string searchTerm, string folderPath, DateTime startDate, DateTime endDate)
        {
            return "3575DFA12CC95DB1C1258527002BBFA7,62A873433324E3C2C1258527003F6490,6AE50A490EBDA3670025868E0058BD13,BEF0F569CEBE78B7002586A400367FFB,00A2D156E200E7E9C12580F3002E1DE9";
        }


        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Privacy()
        {
            return View();
        }
       

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}