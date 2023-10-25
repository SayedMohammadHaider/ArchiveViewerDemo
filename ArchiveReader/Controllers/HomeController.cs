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
        private const string titleJsonFileContent = "[\r\n  {\r\n    \"Docid\": \"3575DFA12CC95DB1C1258527002BBFA7\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:34\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"All good so far All good so far All good so far All good so far All good so far All good so far All good so far All good so far All good so far All good so far All good so far  All good so far\"\r\n  },\r\n  {\r\n    \"Docid\": \"00A2D156E200E7E9C12580F3002E1DE9\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:34\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Search issue file\"\r\n  },\r\n  {\r\n    \"Docid\": \"3575DFA12CC95DB1C1258527002BBFA8\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:34\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Error Sample\"\r\n  },\r\n  {\r\n    \"Docid\": \"DBB9F80A03D9FCC6C1258527003F382A\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:43\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Need information about Unicorns\"\r\n  },\r\n  {\r\n    \"Docid\": \"02C531ED1B5A3056C1258527003F5420\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:53\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"This is amazing\"\r\n  },\r\n  {\r\n    \"Docid\": \"DAF3DA4244F65B48C125779D001C8E82\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:50:53\",\r\n    \"X_Errorcode\": \"Error File\",\r\n    \"XA_Title\": \"Error File\"\r\n  },\r\n  {\r\n    \"Docid\": \"62A873433324E3C2C1258527003F6490\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:02\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Amazing ideas\"\r\n  },\r\n  {\r\n    \"Docid\": \"3771B6E5525D01B200258685005B0951\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:11\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Another document\"\r\n  },\r\n  {\r\n    \"Docid\": \"FF00B4485A52ED7A002586870043AB54\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:20\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"A Document\"\r\n  },\r\n  {\r\n    \"Docid\": \"62CD3D60F358FDDF00258687004A2954\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:30\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"sdsda\"\r\n  },\r\n  {\r\n    \"Docid\": \"F73C078F3F4868290025868C007DD0B1\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:39\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Demo document\"\r\n  },\r\n  {\r\n    \"Docid\": \"6AE50A490EBDA3670025868E0058BD13\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:51:56\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"document with single table\"\r\n  },\r\n  {\r\n    \"Docid\": \"BEF0F569CEBE78B7002586A400367FFB\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:52:18\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Contrail2\"\r\n  },\r\n  {\r\n    \"Docid\": \"8D7AC2B871A9D52000258758003E38B7\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:52:26\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Copy new document\"\r\n  },\r\n  {\r\n    \"Docid\": \"72564FAA249F64D60025875800625178\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:52:35\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"A Document\"\r\n  },\r\n  {\r\n    \"Docid\": \"9905F2A742558D640025877A00460807\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:52:48\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"funny thing\"\r\n  },\r\n  {\r\n    \"Docid\": \"092AADEF2408F50D00258868003D8000\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:53:04\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Sections\"\r\n  },\r\n  {\r\n    \"Docid\": \"2F8ACF93DBBCA6090025886F00256FFD\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:53:13\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Multi Tables\"\r\n  },\r\n  {\r\n    \"Docid\": \"F8B5A22D9FE7D83D00258893003CF58A\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:53:23\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"OLE object document\"\r\n  },\r\n  {\r\n    \"Docid\": \"057641F5E24C4748C8256AC0003233BF\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:53:23\",\r\n    \"X_Errorcode\": \"Document archived\",\r\n    \"XA_Title\": \"Downloaded from azure to test\"\r\n  },\r\n  {\r\n    \"Docid\": \"C1257B18003831A0E16C8F03408C29F9\",\r\n    \"X_Status\": \"M\",\r\n    \"X_Time\": \"2022-09-06 15:53:23\",\r\n    \"X_Errorcode\": \"I-Frame Error\",\r\n    \"XA_Title\": \"Error with I frame\"\r\n  }\r\n]\r\n";
        private const string loadFoldersContent = "[\r\n  {\r\n    \"folderName\": \"TeamsDemo92\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo82\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo72\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo52\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo62\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo42\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo32\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo12\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo20\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo29\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo28\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo27\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo26\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo25\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo24\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo23\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f5\"\r\n  },\r\n  {\r\n    \"folderName\": \"TeamsDemo22\",\r\n    \"folderPath\": \"C:\\\\Adopt EQ\\\\Documents\\\\OneDrive_2022-09-22\\\\Archive Viewer\\\\TeamsDe2.nsf\",\r\n    \"folderId\": \"20a7e8ce-5be6-40e0-afbd-e0afcbe2a8f6\"\r\n  }\r\n]\r\n";
        private const string emlContent = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">\r\n<html>\r\n<head>\r\n<title>Another document</title></head>\r\n<body text=\"#000000\" bgcolor=\"#FFFFFF\"><div id='meta' style=\"display: none\">\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">$UpdatedBy:</td><td class=\"NotesHeaderRowText\"> CN=Mats Jansson/O=mmsolutionz<br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">$Revisions:</td><td class=\"NotesHeaderRowText\"> 2021-02-23 17:36:16<br/></td></tr>\r\n<hr>\r\n</div>\r\n<div id='fields' style=\"display: none\">\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">Form:</td><td class=\"NotesHeaderRowText\"> Item<br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">Title:</td><td class=\"NotesHeaderRowText\"> Another document<br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">Status:</td><td class=\"NotesHeaderRowText\"> Done<br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">Type:</td><td class=\"NotesHeaderRowText\"> <br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">separate:</td><td class=\"NotesHeaderRowText\"> <br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">ItemNo:</td><td class=\"NotesHeaderRowText\"> <br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">DateCreated:</td><td class=\"NotesHeaderRowText\"> 2021-02-23 17:34:21<br/></td></tr>\r\n<tr class=\"NotesHeaderRow\"><td class=\"NotesHeaderRowLabel\">DocUnid:</td><td class=\"NotesHeaderRowText\"> 3771B6E5525D01B200258685005B0951<br/></td></tr>\r\n<hr>\r\n</div>\r\n<table style=\"font-family: Times New Roman; font-size: 12pt;\">\r\n<tr style=\"height: 18px; vertical-align: top; \"><td>&nbsp;</td><td>&nbsp;</td></tr>\r\n</table><br/><div id='message'>\r\n\r\n\r\n<form action=\"\"><img src=\"data:image/jpeg;base64,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\" width=\"84\" height=\"80\"><br>\r\n<br>\r\n<br>\r\n<b><font size=\"6\">List Item</font></b><br>\r\n<br>\r\nCreated: 02/23/2021 04:34:21 PM\r\n<table border=\"1\">\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Title:</font></td><td width=\"448\"><font size=\"4\">Another document</font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Status:</font></td><td width=\"448\"><font size=\"4\">Done</font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">City:</font></td><td width=\"448\"><font size=\"4\">Borås</font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Type: </font></td><td width=\"448\"><font size=\"4\"></font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Separate values:</font></td><td width=\"448\"><font size=\"4\"></font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">ItemNumber:</font></td><td width=\"448\"><font size=\"4\"></font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Translated text</font></td><td width=\"448\"><font size=\"4\">Tuesday</font></td></tr>\r\n\r\n<tr valign=\"top\"><td width=\"228\"><font size=\"4\">Date:</font></td><td width=\"448\"><font size=\"4\"></font></td></tr>\r\n</table>\r\n<font size=\"4\">\t \t\t</font><br>\r\n<font size=\"4\">Info : </font><br>\r\n<b><font size=\"4\">General goal</font></b><br>\r\n<font size=\"4\">Stored XML documents in Azure Storage accounts should be easily readable by the users. It should also be easily searchable both by searching for key words or FT search.</font><br>\r\n<br>\r\n<font size=\"4\">In each and every XML document there is a Tag called &lt;Form&gt; or &lt;FORM&gt;. Depending on the value in this tag, the document should be rendered with the help of a defined XSLT sheet. The output should be readable with a standard browser. </font><br>\r\n<br>\r\n<font size=\"4\">There should be an admin interface to create the XSLT sheets. This should be done in a graphical interface in preferably a standard browser. The function should read the XML file and tell the admin which fields are available. Then in another panel have it possible to drag around the field placeholder and create labels to the fields. </font><br>\r\n<font size=\"4\">Set the name and save.</font><br>\r\n<br>\r\n<font size=\"4\">Räksmörgås</font><br>\r\n<br>\r\n<font size=\"4\">Searching should be available to end users and could use standard search functionalities if possible.</font>\r\n<input name=\"DateCreated\" type=\"hidden\" value=\"02/23/2021 04:34:21 PM\">\r\n<input name=\"Title\" type=\"hidden\" value=\"Another document\">\r\n<input name=\"Status\" type=\"hidden\" value=\"Done\">\r\n<input name=\"City\" type=\"hidden\" value=\"Borås\">\r\n<input name=\"Type\" type=\"hidden\" value=\"\">\r\n<input name=\"separate\" type=\"hidden\" value=\"\">\r\n<input name=\"ItemNo\" type=\"hidden\" value=\"\">\r\n<input name=\"prodname\" type=\"hidden\" value=\"Tuesday\">\r\n<input name=\"DueDate\" type=\"hidden\" value=\"\"></form>\r\n</body>\r\n</html>\r\n";
        
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
            //var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\email.htm");
            return emlContent;
        }

        [HttpGet]
        public async Task<string> LoadFolders()
        {
            //var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\LoadFolders.json");
            return loadFoldersContent;
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
                //var fileData = System.IO.File.ReadAllText(@"..\ArchiveReader\wwwroot\DemoFolder\ReadTitleJsonFIle.json");
                var fileList = JsonConvert.DeserializeObject<List<Files>>(titleJsonFileContent);
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