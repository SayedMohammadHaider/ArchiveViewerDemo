using Microsoft.AspNetCore.Components.Forms;
using MsgReader.Exceptions;
using MsgReader.Helpers;
using MsgReader.Localization;
using MsgReader.Mime;
using MsgReader.Mime.Header;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Archive_Reader
{

    #region Interface IReader
    /// <summary>
    /// Interface to make Reader class COM visible
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Extract the input msg file to the given output folder
        /// </summary>
        /// <param name="inputFile">The msg file</param>
        /// <param name="outputFolder">The folder where to extract the msg file</param>
        /// <param name="hyperlinks">When true then hyperlinks are generated for the To, CC, BCC and attachments</param>
        /// <param name="culture">Sets the culture that needs to be used to localize the output of this class</param>
        /// <returns>String array containing the message body and its (inline) attachments</returns>
        [DispId(1)]
        // ReSharper disable once UnusedMemberInSuper.Global
        string[] ExtractToFolderFromCom(string inputFile, string outputFolder, ReaderHyperLinks hyperlinks = ReaderHyperLinks.None, string culture = "");

        /// <summary>
        /// Get the last know error message. When the string is empty there are no errors
        /// </summary>
        /// <returns></returns>
        [DispId(2)]
        // ReSharper disable once UnusedMemberInSuper.Global
        string GetErrorMessage();
    }
    #endregion

    #region ReaderHyperLinks
    /// <summary>
    /// Tells the readers class when the generate hyperlinks

    /// </summary>
    public enum ReaderHyperLinks
    {
        /// <summary>
        /// Do not generate any hyperlink
        /// </summary>
        None,

        /// <summary>
        /// Only generate hyperlinks for the e-mail addresses
        /// </summary>
        Email,

        /// <summary>
        /// Only generate hyperlinks for the attachments
        /// </summary>
        Attachments,

        /// <summary>
        /// Generate hyperlinks for the e-mail addresses and attachments
        /// </summary>
        Both
    }
    #endregion

    /// <summary>
    /// This class can be used to read an Outlook msg file and save the message body (in HTML or TEXT format)
    /// and all it's attachments to an output folder.
    /// </summary>
    [Guid("E9641DF0-18FC-11E2-BC95-1ACF6088709B")]
    [ComVisible(true)]
    public class Reader : IReader
    {
        #region Fields
        /// <summary>
        /// Contains an error message when something goes wrong in the <see cref="ExtractToFolderFromCom"/> method.
        /// This message can be retrieved with the GetErrorMessage. This way we keep .NET exceptions inside
        /// when this code is called from a COM language
        /// </summary>
        private string _errorMessage;

        /// <summary>
        /// Used to keep track if we already did write an empty line
        /// </summary>
        private static bool _emptyLineWritten;

        /// <summary>
        /// Placeholder for custom header styling
        /// </summary>
        private static string _customHeaderStyleCss;

        public static string xmlHeader;
        #endregion

        #region Properties
        /// <summary>
        ///     An unique id that can be used to identify the logging of the reader when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId
        {
            set => Logger.InstanceId = value;
        }

        /// <summary>
        /// Set / Get whether to use default default styling of email header or
        /// to use the custom CSS style set by <see cref="SetCustomHeaderStyle"/>
        /// </summary>
        public static bool UseCustomHeaderStyle
        {
            get;
            set;
        }
        public static string amlfile;

        /// <summary>
        /// If true the header is injected as an iframe effectively ensuring it is not affected by any css in the message
        /// </summary>
        public static bool InjectHeaderAsIFrame
        {
            get;
            set;
        }
        #endregion

        #region HeaderStyle
        /// <summary>
        /// Set the custom CSS stylesheet for the email header.
        /// Set to string.Empty or null to reset to default. Get current or default CSS via <see cref="GetCustomHeaderStyle"/>
        /// </summary>
        /// <param name="headerStyleCss"></param>
        public static void SetCustomHeaderStyle(string headerStyleCss)
        {
            _customHeaderStyleCss = headerStyleCss;
        }

        /// <summary>
        /// Get current custom CSS stylesheet to apply to email header
        /// </summary>
        /// <returns>Returns default CSS until a custom is set via <see cref="SetCustomHeaderStyle"/></returns>
        public static string GetCustomHeaderStyle()
        {
            if (!string.IsNullOrEmpty(_customHeaderStyleCss))
                return _customHeaderStyleCss;

            // Return defaultStyle
            const string defaultHeaderCss =
                "table.MsgReaderHeader {" +
                "   font-family: Times New Roman; font-size: 12pt;" +
                "}\n" +
                "tr.MsgReaderHeaderRow {" +
                "   height: 18px; vertical-align: top;" +
                "}\n" +
                "tr.MsgReaderHeaderRowEmpty {}\n" +
                "td.MsgReaderHeaderRowLabel {" +
                "   font-weight: bold; white-space:nowrap;" +
                "}\n" +
                "td.MsgReaderHeaderRowText {}\n" +
                "div.MsgReaderContactPhoto {" +
                "   height: 250px; position: absolute; top: 20px; right: 20px;" +
                "}\n" +
                "div.MsgReaderContactPhoto > img {" +
                "   height: 100%;" +
                "}\n" +
                "table.MsgReaderInlineAttachment {" +
                "   width: 70px; display: inline; text-align: center; font-family: Times New Roman; font-size: 12pt;" +
                "}";

            return defaultHeaderCss;
        }
        #endregion

        #region Constructor
        /// <summary>
        ///     Creates this object and sets it's needed properties
        /// </summary>
        /// <param name="logStream">When set then logging is written to this stream for all extractions. If
        /// you want a separate log for each extraction then set the log stream on one of the ExtractTo methods</param>
        public Reader(Stream logStream = null)
        {
            if (logStream != null)
                Logger.LogStream = logStream;
        }
        #endregion

        #region SetCulture
        /// <summary>
        /// Sets the culture that needs to be used to localize the output of this class. 
        /// Default the current system culture is set. When there is no localization available the
        /// default will be used. This will be en-US.
        /// </summary>
        /// <param name="name">The name of the culture eg. nl-NL</param>
        public void SetCulture(string name)
        {
            Logger.WriteToLog($"Setting culture to '{name}'");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }
        #endregion

        #region CheckFileNameAndOutputFolder
        /// <summary>
        /// Checks if the <paramref name="inputFile"/> and <paramref name="outputFolder"/> is valid
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFolder"></param>
        /// <exception cref="ArgumentNullException">Raised when the <paramref name="inputFile"/> or <paramref name="outputFolder"/> is null or empty</exception>
        /// <exception cref="FileNotFoundException">Raised when the <paramref name="inputFile"/> does not exists</exception>
        /// <exception cref="DirectoryNotFoundException">Raised when the <paramref name="outputFolder"/> does not exist</exception>
        /// <exception cref="MRFileTypeNotSupported">Raised when the extension is not .msg or .eml</exception>
        private static string CheckFileNameAndOutputFolder(string inputFile, string outputFolder)
        {
            Logger.WriteToLog("Checking input file and output folder");

            if (string.IsNullOrEmpty(inputFile))
                throw new ArgumentNullException(inputFile);

            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(outputFolder);

            if (!File.Exists(inputFile))
                throw new FileNotFoundException(inputFile);

            if (!Directory.Exists(outputFolder))
                throw new DirectoryNotFoundException(outputFolder);

            var extension = Path.GetExtension(inputFile);
            if (string.IsNullOrEmpty(extension))
                throw new MRFileTypeNotSupported("Expected .msg or .eml extension on the input file");

            extension = extension.ToUpperInvariant();

            using (var fileStream = File.OpenRead(inputFile))
            {
                var header = new byte[2];
                fileStream.Read(header, 0, 2);

                switch (extension)
                {
                    case ".MSG":
                        // Sometimes the email contains an MSG extension and actual it's an EML.
                        // Most of the times this happens when a user saves the email manually and types 
                        // the filename. To prevent these kind of errors we do a double check to make sure 
                        // the file is really an MSG file
                        if (header[0] == 0xD0 && header[1] == 0xCF)
                            return ".MSG";

                        return ".EML";

                    case ".EML":
                        // We can't do an extra check over here because an EML file is text based 
                        return extension;
                    case ".AML":
                        // We can't do an extra check over here because an EML file is text based 
                        return extension;

                    default:
                        const string message = "Wrong file extension, expected .msg or .eml";
                        throw new MRFileTypeNotSupported(message);
                }
            }
        }
        #endregion

        #region ExtractToStream
        /// <summary>
        /// This method reads the <paramref name="inputStream"/> and when the stream is supported it will do the following: <br/>
        /// - Extract the HTML, RTF (will be converted to html) or TEXT body (in these order) <br/>
        /// - Puts a header (with the sender, to, cc, etc... (depends on the message type) on top of the body so it looks
        ///   like if the object is printed from Outlook <br/>
        /// - Reads all the attachments <br/>
        /// And in the end returns everything to the output stream
        /// </summary>
        /// <param name="inputStream">The mime stream</param>
        /// <param name="hyperlinks">When true hyperlinks are generated for the To, CC, BCC and attachments</param>
        public List<MemoryStream> ExtractToStream(MemoryStream inputStream, bool hyperlinks = false)
        {
            var message = Message.Load(inputStream);
            return WriteEmlStreamEmail(message, hyperlinks);
        }
        #endregion

        #region ExtractToFolder
        /// <summary>
        /// This method reads the <paramref name="inputFile"/> and when the file is supported it will do the following: <br/>
        /// - Extract the HTML, RTF (will be converted to html) or TEXT body (in these order) <br/>
        /// - Puts a header (with the sender, to, cc, etc... (depends on the message type) on top of the body so it looks 
        ///   like if the object is printed from Outlook <br/>
        /// - Reads all the attachments <br/>
        /// And in the end writes everything to the given <paramref name="outputFolder"/>
        /// </summary>
        /// <param name="inputFile">The msg file</param>
        /// <param name="outputFolder">The folder where to save the extracted msg file</param>
        /// <param name="hyperlinks"><see cref="ReaderHyperLinks"/></param>
        /// <param name="culture"></param>
        public string[] ExtractToFolderFromCom(string inputFile,
            string outputFolder,
            ReaderHyperLinks hyperlinks = ReaderHyperLinks.None,
            string culture = "")
        {
            Console.WriteLine("or here");
            try
            {
                if (!string.IsNullOrEmpty(culture))
                    SetCulture(culture);

                return ExtractToFolder(inputFile, outputFolder, hyperlinks);
            }
            catch (Exception e)
            {
                _errorMessage = ExceptionHelpers.GetInnerException(e);
                return new string[0];
            }
        }

        /// <summary>
        /// This method reads the <paramref name="inputFile"/> and when the file is supported it will do the following: <br/>
        /// - Extract the HTML, RTF (will be converted to html) or TEXT body (in these order) <br/>
        /// - Puts a header (with the sender, to, cc, etc... (depends on the message type) on top of the body so it looks 
        ///   like if the object is printed from Outlook <br/>
        /// - Reads all the attachments <br/>
        /// And in the end writes everything to the given <paramref name="outputFolder"/>
        /// </summary>
        /// <param name="inputFile">The msg file</param>
        /// <param name="outputFolder">The folder where to save the extracted msg file</param>
        /// <param name="hyperlinks"><see cref="ReaderHyperLinks"/></param>
        /// <param name="messageType">Use this if you get the exception <see cref="MRFileTypeNotSupported"/> and
        /// want to force this method to use a specific <see cref="MessageType"/> to parse this MSG file. This
        /// is only used when the file is an MSG file</param>
        /// <param name="logStream">When set then this will give a logging for each extraction. Use the log stream
        /// option in the constructor if you want one log for all extractions</param>/// 
        /// <returns>String array containing the full path to the message body and its attachments</returns>
        /// <exception cref="MRFileTypeNotSupported">Raised when the Microsoft Outlook message type is not supported</exception>
        /// <exception cref="MRInvalidSignedFile">Raised when the Microsoft Outlook signed message is invalid</exception>
        /// <exception cref="ArgumentNullException">Raised when the <param ref="inputFile"/> or <param ref="outputFolder"/> is null or empty</exception>
        /// <exception cref="FileNotFoundException">Raised when the <param ref="inputFile"/> does not exists</exception>
        /// <exception cref="DirectoryNotFoundException">Raised when the <param ref="outputFolder"/> does not exists</exception>
        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public string[] ExtractToFolder(
            string inputFile,
            string outputFolder,
            ReaderHyperLinks hyperlinks = ReaderHyperLinks.None)
        {

            amlfile = inputFile;
            outputFolder = FileManager.CheckForDirectorySeparator(outputFolder);

            _errorMessage = string.Empty;


            Console.WriteLine($"Extracting AML file '{inputFile}' to output folder '{outputFolder}'");
            using (var stream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Console.WriteLine($"message1 {stream}");
                var message = Message.Load(stream);
                Console.WriteLine($"message2 {message}");
                return WriteEmlEmail(message, outputFolder, hyperlinks).ToArray();
            }
        }
        #endregion





        #region ReplaceFirstOccurence
        /// <summary>
        /// Method to replace the first occurence of the <paramref name="search"/> string with a
        /// <paramref name="replace"/> string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="search"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        private static string ReplaceFirstOccurence(string text, string search, string replace)
        {
            var index = text.IndexOf(search, StringComparison.Ordinal);
            if (index < 0)
                return text;

            return text.Substring(0, index) + replace + text.Substring(index + search.Length);
        }
        #endregion






        #region WriteHeader methods
        /// <summary>
        /// Surrounds the String with HTML tags
        /// </summary>
        /// <param name="footer"></param>
        /// <param name="htmlBody"></param>
        private static void SurroundWithHtml(StringBuilder footer, bool htmlBody)
        {
            if (!htmlBody)
                return;
            footer.Insert(0, "<html><script></script><body><br/>");
            footer.AppendLine("</body></html>");

            _emptyLineWritten = false;
        }

        /// <summary>
        /// Writes the start of the header
        /// </summary>
        /// <param name="header">The <see cref="StringBuilder"/> object that is used to write a header</param>
        /// <param name="htmlBody">When true then html will be written into the <param ref="header"/> otherwise text will be written</param>
        private static void WriteHeaderStart(StringBuilder header, bool htmlBody)
        {
            if (!htmlBody)
                return;

            if (UseCustomHeaderStyle)
            {
                header.AppendLine("<style>" + GetCustomHeaderStyle() + "</style>");
                header.AppendLine("<table class=\"MsgReaderHeader\">");
            }
            else
                header.AppendLine("<table style=\"font-family: Times New Roman; font-size: 12pt;\">");

            _emptyLineWritten = false;
        }

        /// <summary>
        /// Writes a line into the header
        /// </summary>
        /// <param name="header">The <see cref="StringBuilder"/> object that is used to write a header</param>
        /// <param name="htmlBody">When true then html will be written into the <paramref name="header"/> otherwise text will be written</param>
        /// <param name="labelPadRightWidth">Used to pad the label size, ignored when <paramref name="htmlBody"/> is true</param>
        /// <param name="label">The label text that needs to be written</param>
        /// <param name="text">The text that needs to be written after the <paramref name="label"/></param>
        private static void WriteNotesHeaderLine(StringBuilder header,
            bool htmlBody,
            int labelPadRightWidth,
            string label,
            string text)
        {
            if (htmlBody)
            {
                var lines = text.Split('\n');
                var newText = string.Empty;

                foreach (var line in lines)
                    newText += WebUtility.HtmlEncode(line) + "<br/>";

                string htmlTr;
                htmlTr =
                        "<tr class=\"NotesHeaderRow\">" +
                        "<td class=\"NotesHeaderRowLabel\">";


                htmlTr += WebUtility.HtmlEncode(label) + ":</td>" +
                          "<td class=\"NotesHeaderRowText\">" + newText + "</td></tr>";

                header.AppendLine(htmlTr);
            }
            else
            {
                text = text.Replace("\n", "".PadRight(labelPadRightWidth));
                header.AppendLine((label + ":").PadRight(labelPadRightWidth) + text);
            }

            _emptyLineWritten = false;
        }
        private static void WriteHorizontalLine(StringBuilder header, bool htmlBody, int labelPadRightWidth)
        {
            if (htmlBody)
            {
                string htmlTr;

                if (UseCustomHeaderStyle)
                {
                    htmlTr =
                        "<hr>";
                }
                else
                {
                    htmlTr =
                        "<hr>";
                }


                header.AppendLine(htmlTr);
            }


            _emptyLineWritten = false;
        }
        private static void WriteFieldStartDiv(StringBuilder header, bool htmlBody, int labelPadRightWidth)
        {
            if (htmlBody)
            {
                string htmlTr;
                htmlTr = "<div id='meta' style=\"display: none\">";
                header.AppendLine(htmlTr);
            }

            _emptyLineWritten = false;
        }
        private static void WriteFieldStartDivFields(StringBuilder header, bool htmlBody, int labelPadRightWidth)
        {
            if (htmlBody)
            {
                string htmlTr;
                htmlTr = "<div id='fields' style=\"display: none\">";
                header.AppendLine(htmlTr);
            }

            _emptyLineWritten = false;
        }
        private static void WriteFieldEndDiv(StringBuilder header, bool htmlBody, int labelPadRightWidth)
        {
            if (htmlBody)
            {
                string htmlTr;
                htmlTr = "</div>";
                header.AppendLine(htmlTr);
            }

            _emptyLineWritten = false;
        }
        private static void WriteButton(StringBuilder header, bool htmlBody, int labelPadRightWidth)
        {
            if (htmlBody)
            {
                string htmlTr;

                htmlTr = "<button type=\"button\" onclick=\"toggle()\">Notes fields</button>&nbsp<button type=\"button\" onclick=\"togglemeta()\">Notes headers</button><hr>";

                header.AppendLine(htmlTr);
            }


            _emptyLineWritten = false;
        }
        private static void WriteHeaderLine(StringBuilder header,
           bool htmlBody,
           int labelPadRightWidth,
           string label,
           string text)
        {
            if (htmlBody)
            {
                var lines = text.Split('\n');
                var newText = string.Empty;

                foreach (var line in lines)
                    newText += WebUtility.HtmlEncode(line) + "<br/>";

                string htmlTr;

                if (UseCustomHeaderStyle)
                {
                    htmlTr =
                        "<tr class=\"MsgReaderHeaderRow\">" +
                        "<td class=\"MsgReaderHeaderRowLabel\">";
                }
                else
                {
                    htmlTr =
                        "<tr style=\"height: 18px; vertical-align: top; \"><td style=\"font-weight: bold; white-space:nowrap;\">";
                }

                htmlTr += WebUtility.HtmlEncode(label) + ":</td>" +
                          "<td class=\"MsgReaderHeaderRowText\">" + newText + "</td></tr>";

                header.AppendLine(htmlTr);
            }
            else
            {
                text = text.Replace("\n", "".PadRight(labelPadRightWidth));
                header.AppendLine((label + ":").PadRight(labelPadRightWidth) + text);
            }

            _emptyLineWritten = false;
        }

        /// <summary>
        /// Writes a line into the header without Html encoding the <paramref name="text"/>
        /// </summary>
        /// <param name="header">The <see cref="StringBuilder"/> object that is used to write a header</param>
        /// <param name="htmlBody">When true then html will be written into the <paramref name="header"/> otherwise text will be written</param>
        /// <param name="labelPadRightWidth">Used to pad the label size, ignored when <paramref name="htmlBody"/> is true</param>
        /// <param name="label">The label text that needs to be written</param>
        /// <param name="text">The text that needs to be written after the <paramref name="label"/></param>
        private static void WriteHeaderLineNoEncoding(StringBuilder header,
                                                      bool htmlBody,
                                                      int labelPadRightWidth,
                                                      string label,
                                                      string text)
        {
            if (htmlBody)
            {
                text = text.Replace("\n", "<br/>");

                string htmlTr;

                if (UseCustomHeaderStyle)
                {
                    htmlTr =
                        "<tr class=\"MsgReaderHeaderRow\">" +
                        "<td class=\"MsgReaderHeaderRowLabel\">";
                }
                else
                {
                    htmlTr =
                        "<tr style=\"height: 18px; vertical-align: top; \">" +
                        "<td style=\"font-weight: bold; white-space:nowrap;\">";
                }

                htmlTr += WebUtility.HtmlEncode(label) + ":</td>" +
                          "<td class=\"MsgReaderHeaderRowText\">" + text + "</td>" +
                          "</tr>";

                header.AppendLine(htmlTr);
            }
            else
            {
                text = text.Replace("\n", "".PadRight(labelPadRightWidth));
                header.AppendLine((label + ":").PadRight(labelPadRightWidth) + text);
            }

            _emptyLineWritten = false;
        }

        /// <summary>
        /// Writes an empty header line
        /// </summary>
        /// <param name="header"></param>
        /// <param name="htmlBody"></param>
        private static void WriteHeaderEmptyLine(StringBuilder header, bool htmlBody)
        {
            // Prevent that we write 2 empty lines in a row
            if (_emptyLineWritten)
                return;

            if (!htmlBody)
                header.AppendLine(string.Empty);
            else
            {

                header.AppendLine(
                    UseCustomHeaderStyle
                        ? "<tr class=\"MsgReaderHeaderRow MsgReaderHeaderRowEmpty\">" +
                          "<td class=\"MsgReaderHeaderRowLabel\">&nbsp;</td>" +
                          "<td class=\"MsgReaderHeaderRowText\">&nbsp;</td>" +
                          "</tr>"
                        : "<tr style=\"height: 18px; vertical-align: top; \">" +
                          "<td>&nbsp;</td>" +
                          "<td>&nbsp;</td>" +
                          "</tr>");
            }

            _emptyLineWritten = true;
        }

        /// <summary>
        /// Writes the end of the header
        /// </summary>
        /// <param name="header">The <see cref="StringBuilder"/> object that is used to write a header</param>
        /// <param name="htmlBody">When true then html will be written into the <param ref="header"/> otherwise text will be written</param>
        private static void WriteHeaderEnd(StringBuilder header, bool htmlBody)
        {
            header.AppendLine(!htmlBody ? string.Empty : "</table><br/>");
        }
        #endregion



        #region WriteEmlStreamEmail
        /// <summary>
        /// Writes the body of the MSG E-mail to html or text and extracts all the attachments. The
        /// result is returned as a List of MemoryStream
        /// </summary>
        /// <param name="message">The <see cref="Mime.Message"/> object</param>
        /// <param name="hyperlinks">When true then hyperlinks are generated for the To, CC, BCC and attachments</param>
        /// <returns></returns>
        public string sub;
        public List<MemoryStream> WriteEmlStreamEmail(Message message, bool hyperlinks)
        {
            Logger.WriteToLog("Writing EML message body to stream");

            var streams = new List<MemoryStream>();

            PreProcessEmlStream(message,
                hyperlinks,
                out var htmlBody,
                out var body,
                out var attachmentList,
                out var attachStreams);

            if (!htmlBody)
                hyperlinks = false;

            var maxLength = 0;

            // Calculate padding width when we are going to write a text file
            if (!htmlBody)
            {
                var languageConsts = new List<string>
                {
                    #region LanguageConsts
                    LanguageConsts.EmailFromLabel,
                    LanguageConsts.EmailSentOnLabel,
                    LanguageConsts.EmailToLabel,
                    LanguageConsts.EmailCcLabel,
                    LanguageConsts.EmailBccLabel,
                    LanguageConsts.EmailSubjectLabel,
                    LanguageConsts.ImportanceLabel,
                    LanguageConsts.EmailAttachmentsLabel,
                    #endregion
                };

                maxLength = languageConsts.Select(languageConst => languageConst.Length).Concat(new[] { 0 }).Max() + 2;
            }

            /*******************************Start Header*******************************/
            Logger.WriteToLog("Start writing EML header information");

            var emailHeader = new StringBuilder();
            var headers = message.Headers;

            // Start of table
            WriteHeaderStart(emailHeader, htmlBody);
            Console.WriteLine("start header");
            // From
            var from = string.Empty;
            if (headers.From != null)
            {
                from = message.GetEmailAddresses(new List<RfcMailAddress> { headers.From }, hyperlinks, htmlBody);
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, LanguageConsts.EmailFromLabel, from);

                WriteHeaderLine(emailHeader, htmlBody, maxLength, LanguageConsts.EmailSentOnLabel, message.Headers.DateSent.ToLocalTime().ToString(LanguageConsts.DataFormatWithTime));
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, LanguageConsts.EmailToLabel, message.GetEmailAddresses(headers.To, hyperlinks, htmlBody));
            }


            // CC
            var cc = message.GetEmailAddresses(headers.Cc, hyperlinks, htmlBody);
            if (!string.IsNullOrEmpty(cc))
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, LanguageConsts.EmailCcLabel, cc);

            // BCC
            var bcc = message.GetEmailAddresses(headers.Bcc, hyperlinks, htmlBody);
            if (!string.IsNullOrEmpty(bcc))
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, LanguageConsts.EmailBccLabel, bcc);

            // Subject
            var subject = message.Headers.Subject ?? string.Empty;

            WriteHeaderLine(emailHeader, htmlBody, maxLength, LanguageConsts.EmailSubjectLabel, subject);

            // Urgent
            var importanceText = string.Empty;
            switch (message.Headers.Importance)
            {
                case MailPriority.Low:
                    importanceText = LanguageConsts.ImportanceLowText;
                    break;

                case MailPriority.Normal:
                    importanceText = LanguageConsts.ImportanceNormalText;
                    break;

                case MailPriority.High:
                    importanceText = LanguageConsts.ImportanceHighText;
                    break;
            }

            if (!string.IsNullOrEmpty(importanceText))
            {
                WriteHeaderLine(emailHeader, htmlBody, maxLength, LanguageConsts.ImportanceLabel, importanceText);

                // Empty line
                WriteHeaderEmptyLine(emailHeader, htmlBody);
            }

            // Attachments
            if (attachmentList.Count != 0)
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, LanguageConsts.EmailAttachmentsLabel,
                    string.Join(", ", attachmentList));

            // Empty line
            WriteHeaderEmptyLine(emailHeader, htmlBody);

            // End of table + empty line
            WriteHeaderEnd(emailHeader, htmlBody);

            body = InjectHeader(body, emailHeader.ToString());
            //body = InjectHeader(body, "<hr>");

            streams.Add(new MemoryStream(Encoding.UTF8.GetBytes(body)));

            Logger.WriteToLog("End writing EML header information");

            /*******************************End Header*********************************/

            streams.AddRange(attachStreams);

            /*******************************Start Footer*******************************/
            Logger.WriteToLog("Start writing EML footer information");
            var emailFooter = new StringBuilder();

            WriteHeaderStart(emailFooter, htmlBody);
            int i = 0;
            foreach (var item in headers.UnknownHeaders.AllKeys)
            {
                //Console.WriteLine($"Unknown header {item}");
                WriteHeaderLine(emailFooter, htmlBody, maxLength, item, headers.UnknownHeaders[i]);
                i++;
            }


            SurroundWithHtml(emailFooter, htmlBody);
            streams.Add(new MemoryStream(Encoding.UTF8.GetBytes(emailFooter.ToString())));

            /*******************************End Header*********************************/

            return streams;
        }
        #endregion

        #region WriteEmlEmail
        /// <summary>
        /// Writes the body of the EML E-mail to html or text and extracts all the attachments. The
        /// result is returned as a List of strings
        /// </summary>
        /// <param name="message">The <see cref="Mime.Message"/> object</param>
        /// <param name="outputFolder">The folder where we need to write the output</param>
        /// <param name="hyperlinks"><see cref="ReaderHyperLinks"/></param>
        /// <returns></returns>
        private static string DecodeQuotedPrintables(string input, string charSet)
        {
            if (string.IsNullOrEmpty(charSet))
            {
                var charSetOccurences = new Regex(@"=\?.*\?Q\?", RegexOptions.IgnoreCase);
                var charSetMatches = charSetOccurences.Matches(input);
                foreach (Match match in charSetMatches)
                {
                    charSet = match.Groups[0].Value.Replace("=?", "").Replace("?Q?", "");
                    input = input.Replace(match.Groups[0].Value, "").Replace("?=", "");
                }
            }

            Encoding enc = new ASCIIEncoding();
            if (!string.IsNullOrEmpty(charSet))
            {
                try
                {
                    enc = Encoding.GetEncoding(charSet);
                }
                catch
                {
                    enc = new ASCIIEncoding();
                }
            }

            //decode iso-8859-[0-9]
            var occurences = new Regex(@"=[0-9A-Z]{2}", RegexOptions.Multiline);
            var matches = occurences.Matches(input);
            foreach (Match match in matches)
            {
                try
                {
                    byte[] b = new byte[] { byte.Parse(match.Groups[0].Value.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier) };
                    char[] hexChar = enc.GetChars(b);
                    input = input.Replace(match.Groups[0].Value, hexChar[0].ToString());
                }
                catch { }
            }

            //decode base64String (utf-8?B?)
            occurences = new Regex(@"\?utf-8\?B\?.*\?", RegexOptions.IgnoreCase);
            matches = occurences.Matches(input);
            foreach (Match match in matches)
            {
                byte[] b = Convert.FromBase64String(match.Groups[0].Value.Replace("?utf-8?B?", "").Replace("?UTF-8?B?", "").Replace("?", ""));
                string temp = Encoding.UTF8.GetString(b);
                input = input.Replace(match.Groups[0].Value, temp);
            }

            input = input.Replace("=\r\n", "");
            return input;
        }
        private List<string> WriteEmlEmail(Message message, string outputFolder, ReaderHyperLinks hyperlinks)
        {
            //Logger.WriteToLog("Start writing EML e-mail body and attachments to outputfolder");
            Console.WriteLine($"Start writing EML e-mail body and attachments to outputfolder {message}");
            var fileName = "email";

            PreProcessEmlFile(message,
                hyperlinks,
                outputFolder,
                ref fileName,
                out var htmlBody,
                out var body,
                out var attachmentList,
                out var files);

            var convertToHref = true;

            if (htmlBody)
            {
                //Console.WriteLine($"is htmlbody {htmlBody}");
                switch (hyperlinks)
                {
                    case ReaderHyperLinks.Email:
                        convertToHref = true;
                        break;
                    case ReaderHyperLinks.Both:
                        convertToHref = true;
                        break;
                }
            }

            var maxLength = 0;

            // Calculate padding width when we are going to write a text file
            if (!htmlBody)
            {
                Console.WriteLine("is not htmlbody");
                var languageConsts = new List<string>
                {
                    #region LanguageConsts
                    LanguageConsts.EmailFromLabel,
                    LanguageConsts.EmailSentOnLabel,
                    LanguageConsts.EmailToLabel,
                    LanguageConsts.EmailCcLabel,
                    LanguageConsts.EmailBccLabel,
                    LanguageConsts.EmailSubjectLabel,
                    LanguageConsts.ImportanceLabel,
                    LanguageConsts.EmailAttachmentsLabel,
                    LanguageConsts.EmailSignedBy,
                    LanguageConsts.EmailSignedByOn
                    #endregion
                };

                maxLength = languageConsts.Select(languageConst => languageConst.Length).Concat(new[] { 0 }).Max() + 2;
            }

            //Logger.WriteToLog("Start writing EML headers");
            Console.WriteLine("Start writing EML headers");
            var emailHeader = new StringBuilder();

            var headers = message.Headers;
            WriteButton(emailHeader, htmlBody, maxLength);
            if (headers.NotesItems != null)
            {
                WriteFieldStartDiv(emailHeader, htmlBody, maxLength);
                foreach (KeyValuePair<string, string> pair in headers.NotesItems)
                {
                    string key = pair.Key.ToString();
                    if (key.StartsWith("$"))
                    {
                        string value = pair.Value.ToString();
                        //Console.WriteLine($"noteskey  {key} - {value}");
                        WriteNotesHeaderLine(emailHeader, htmlBody, maxLength, key, $"{value}");
                    }
                }
                WriteHorizontalLine(emailHeader, htmlBody, maxLength);
                WriteFieldEndDiv(emailHeader, htmlBody, maxLength);

                WriteFieldStartDivFields(emailHeader, htmlBody, maxLength);
                foreach (KeyValuePair<string, string> pair in headers.NotesItems)
                {
                    string key = pair.Key.ToString();
                    if (!key.StartsWith("$"))
                    {
                        string value = pair.Value.ToString();

                        if (value.StartsWith("=?ISO-8859-1"))
                        {
                            value = DecodeQuotedPrintables(value, "UTF-8");
                            value = value.Replace("=?ISO-8859-1?Q?", "");
                            value = value.Replace("?=", "");
                        }

                        //Console.WriteLine($"notes {key} - {value}");
                        WriteNotesHeaderLine(emailHeader, htmlBody, maxLength, key, $"{value}");
                    }
                }
                WriteHorizontalLine(emailHeader, htmlBody, maxLength);
                WriteFieldEndDiv(emailHeader, htmlBody, maxLength);
            }
            // Start of table
            WriteHeaderStart(emailHeader, htmlBody);

            // From
            var from = string.Empty;
            if (headers.From != null)
            {
                from = message.GetEmailAddresses(new List<RfcMailAddress> { headers.From }, convertToHref, htmlBody);
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailFromLabel", from);
                WriteHeaderLine(emailHeader, htmlBody, maxLength, "EmailSentOnLabel", message.Headers.DateSent.ToLocalTime().ToString("DataFormatWithTime"));
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailToLabel", message.GetEmailAddresses(headers.To, convertToHref, htmlBody));
            }


            // CC

            var cc = message.GetEmailAddresses(headers.Cc, convertToHref, htmlBody);
            if (!string.IsNullOrEmpty(cc))
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailCcLabel", cc);

            // BCC
            var bcc = message.GetEmailAddresses(headers.Bcc, convertToHref, htmlBody);
            if (!string.IsNullOrEmpty(bcc))
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailBccLabel", bcc);

            if (message.SignedBy != null)
            {
                var signerInfo = message.SignedBy;
                if (message.SignedOn != null)
                {
                    signerInfo += " " + "EmailSignedByOn" + " " +
                                  ((DateTime)message.SignedOn).ToString("DataFormatWithTime");

                    WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailSignedBy",
                        signerInfo);
                }
            }

            // Subject
            if (headers.Subject != null)
            {
                var subject = message.Headers.Subject ?? string.Empty;
                WriteHeaderLine(emailHeader, htmlBody, maxLength, "EmailSubjectLabel", subject);
            }
            // Urgent
            var importanceText = string.Empty;
            switch (message.Headers.Importance)
            {
                case MailPriority.Low:
                    importanceText = "ImportanceLowText";
                    break;

                case MailPriority.Normal:
                    importanceText = "ImportanceNormalText";
                    break;

                case MailPriority.High:
                    importanceText = "ImportanceHighText";
                    break;
            }

            if (!string.IsNullOrEmpty(importanceText))
            {
                WriteHeaderLine(emailHeader, htmlBody, maxLength, "ImportanceLabel", importanceText);

                // Empty line
                WriteHeaderEmptyLine(emailHeader, htmlBody);
            }

            // Attachments
            if (attachmentList.Count != 0)
                WriteHeaderLineNoEncoding(emailHeader, htmlBody, maxLength, "EmailAttachmentsLabel",
                    string.Join(", ", attachmentList));

            // Empty line
            WriteHeaderEmptyLine(emailHeader, htmlBody);

            // End of table + empty line
            WriteHeaderEnd(emailHeader, htmlBody);

            //Logger.WriteToLog("Stop writing EML headers");
            Console.WriteLine("Stop writing EML headers");
            //body = InjectHeader(body, "<script>function toggle() {if (document.getElementById(\"hidethis\").style.display == 'none'){document.getElementById(\"hidethis\").style.display = '';}else{document.getElementById(\"hidethis\").style.display = 'none';}}</script>");

            body = InjectHeader(body, emailHeader.ToString());
            //Console.WriteLine($"body {body}");

            body = body.Replace("]]></mime></document>", "");


            // Write the body to a file
            File.WriteAllText(fileName, body, Encoding.UTF8);

            Console.WriteLine("Stop writing EML e-mail body and attachments to outputfolder");

            return files;
        }
        #endregion

        #region PreProcessEmlStream
        /// <summary>
        /// This function pre processes the EML <see cref="Mime.Message"/> object, it tries to find the html (or text) body
        /// and reads all the available <see cref="Mime.MessagePart">attachment</see> objects. When an attachment is inline it tries to
        /// map this attachment to the html body part when this is available
        /// </summary>
        /// <param name="message">The <see cref="Mime.Message"/> object</param>
        /// <param name="hyperlinks">When true then hyperlinks are generated for the To, CC, BCC and
        /// attachments (when there is an html body)</param>
        /// <param name="htmlBody">Returns true when the <see cref="Mime.Message"/> object did contain
        /// an HTML body</param>
        /// <param name="body">Returns the html or text body</param>
        /// <param name="attachments">Returns a list of names with the found attachment</param>
        /// <param name="attachStreams">Returns all the attachments as a list of streams</param>
        public void PreProcessEmlStream(Message message,
            bool hyperlinks,
            out bool htmlBody,
            out string body,
            out List<string> attachments,
            out List<MemoryStream> attachStreams)
        {
            Console.WriteLine("Start pre processing EML stream");

            attachments = new List<string>();
            attachStreams = new List<MemoryStream>();

            var bodyMessagePart = message.HtmlBody;

            if (bodyMessagePart != null)
            {
                Console.WriteLine("Getting HTML body");
                body = bodyMessagePart.GetBodyAsText();
                htmlBody = true;
            }
            else
            {
                bodyMessagePart = message.TextBody;

                // When there is no body at all we just make an empty html document
                if (bodyMessagePart != null)
                {
                    Console.WriteLine("Getting TEXT body");
                    body = bodyMessagePart.GetBodyAsText();
                    htmlBody = false;
                }
                else
                {
                    Console.WriteLine("No body found, making an empty HTML body");
                    htmlBody = true;
                    body = "<html><head></head><body></body></html>";
                }
            }

            Console.WriteLine("Stop getting body");

            if (message.Attachments != null)
            {
                Console.WriteLine("Start processing attachments");
                foreach (var attachment in message.Attachments)
                {
                    var attachmentFileName = attachment.FileName;

                    //use the stream here and don't worry about needing to close it
                    attachStreams.Add(new MemoryStream(attachment.Body));

                    // When we find an inline attachment we have to replace the CID tag inside the html body
                    // with the name of the inline attachment. But before we do this we check if the CID exists.
                    // When the CID does not exists we treat the inline attachment as a normal attachment
                    if (htmlBody && !string.IsNullOrEmpty(attachment.ContentId) && body.Contains(attachment.ContentId))
                    {
                        Console.WriteLine("Attachment is inline");
                        body = body.Replace("cid:" + attachment.ContentId, CheckValidAttachment(attachmentFileName));
                    }
                    else
                    {
                        // If we didn't find the cid tag we treat the inline attachment as a normal one

                        if (htmlBody)
                        {
                            Console.WriteLine($"Attachment was marked as inline but the body did not contain the content id '{attachment.ContentId}' so mark it as a normal attachment");

                            if (hyperlinks)
                                attachments.Add("<a href=\"" + attachmentFileName + "\">" +
                                                HttpUtility.HtmlEncode(CheckValidAttachment(attachmentFileName)) + "</a> (" +
                                                FileManager.GetFileSizeString(attachment.Body.Length) + ")");
                            else
                                attachments.Add(HttpUtility.HtmlEncode(CheckValidAttachment(attachmentFileName)) + " (" +
                                                FileManager.GetFileSizeString(attachment.Body.Length) + ")");
                        }
                        else
                            attachments.Add(CheckValidAttachment(attachmentFileName) + " (" +
                                            FileManager.GetFileSizeString(attachment.Body.Length) + ")");
                    }

                    Console.WriteLine($"Attachment written to '{attachmentFileName}' with size '{FileManager.GetFileSizeString(attachment.Body.Length)}'");
                }

                Console.WriteLine("Start processing attachments");
            }
            else
                Console.WriteLine("E-mail does not contain any attachments");


            Console.WriteLine("Stop pre processing EML stream");
        }
        #endregion

        #region CheckValidAttachment
        /// <summary>
        /// Check for Valid Attachment
        /// </summary>
        /// <param name="attachmentFileName"></param>
        /// <returns></returns>
        public string CheckValidAttachment(string attachmentFileName)
        {
            var filename = attachmentFileName;
            var attachType = Path.GetExtension(attachmentFileName);
            switch (attachType)
            {
                case ".txt":
                case ".rtf":
                case ".doc":
                case ".docx":
                case ".pdf":
                case ".jpg":
                case ".tif":
                case ".tiff":
                case ".png":
                case ".wmf":
                case ".gif":
                    filename = attachmentFileName;
                    break;
                default:
                    filename = filename + " (This attachment is not a supported attachment type.)";
                    break;
            }
            return filename;
        }
        #endregion

        #region PreProcessEmlFile
        /// <summary>
        /// This function pre processes the EML <see cref="Mime.Message"/> object, it tries to find the html (or text) body
        /// and reads all the available <see cref="Mime.MessagePart">attachment</see> objects. When an attachment is inline it tries to
        /// map this attachment to the html body part when this is available
        /// </summary>
        /// <param name="message">The <see cref="Mime.Message"/> object</param>
        /// <param name="hyperlinks"><see cref="ReaderHyperLinks"/></param>
        /// <param name="outputFolder">The output folder where all extracted files need to be written</param>
        /// <param name="fileName">Returns the filename for the html or text body</param>
        /// <param name="htmlBody">Returns true when the <see cref="Mime.Message"/> object did contain 
        /// an HTML body</param>
        /// <param name="body">Returns the html or text body</param>
        /// <param name="attachments">Returns a list of names with the found attachment</param>
        /// <param name="files">Returns all the files that are generated after pre processing the <see cref="Mime.Message"/> object</param>
        private static void PreProcessEmlFile(Message message,
            ReaderHyperLinks hyperlinks,
            string outputFolder,
            ref string fileName,
            out bool htmlBody,
            out string body,
            out List<string> attachments,
            out List<string> files)
        {
            Console.WriteLine("Start pre processing EML file");

            attachments = new List<string>();
            files = new List<string>();

            var bodyMessagePart = message.HtmlBody;

            if (bodyMessagePart != null)
            {

                body = bodyMessagePart.GetBodyAsText();

                Console.WriteLine($"Getting HTML body ");
                if (body.Length == 0)
                {
                    string content;
                    using (var sr = new StreamReader(amlfile))
                    {
                        content = sr.ReadToEnd();

                        if (content.Contains("PGh0"))
                        {
                            string b64 = getBetween(content, "PGh", "==");
                            string fullstring = $"PGh{b64}==";
                            //Console.WriteLine(fullstring);
                            body = Base64Decode(fullstring);
                            // Console.WriteLine(ctcontents);

                        }
                    }
                }
                htmlBody = true;
            }
            else
            {
                bodyMessagePart = message.TextBody;

                // When there is no body at all we just make an empty html document
                if (bodyMessagePart != null)
                {
                    Console.WriteLine("Getting TEXT body");
                    body = bodyMessagePart.GetBodyAsText();
                    htmlBody = false;
                }
                else
                {
                    Console.WriteLine("No body found, making an empty HTML body");
                    htmlBody = true;
                    body = "<html><head>44</head><body></body></html>";
                }
            }

            var subject = string.Empty;

            if (message.Headers.Subject != null)
                subject = FileManager.RemoveInvalidFileNameChars(message.Headers.Subject);

            fileName = outputFolder +
                       (!string.IsNullOrEmpty(subject)
                           ? subject
                           : fileName) + (htmlBody ? ".htm" : ".txt");

            fileName = FileManager.FileExistsMakeNew(fileName);

            Console.WriteLine($"Body written to '{fileName}'");

            files.Add(fileName);

            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var attachmentFileName = attachment.FileName;
                    var fileInfo = new FileInfo(FileManager.FileExistsMakeNew(outputFolder + attachmentFileName));
                    File.WriteAllBytes(fileInfo.FullName, attachment.Body);

                    // When we find an inline attachment we have to replace the CID tag inside the html body
                    // with the name of the inline attachment. But before we do this we check if the CID exists.
                    // When the CID does not exists we treat the inline attachment as a normal attachment
                    if (htmlBody && attachment.IsInline &&
                        (!string.IsNullOrEmpty(attachment.ContentId) && body.Contains($"cid:{attachment.ContentId}") ||
                         body.Contains($"cid:{attachment.FileName}")))
                    {
                        Console.WriteLine("Attachment is inline");

                        body = !string.IsNullOrEmpty(attachment.ContentId)
                            ? body.Replace("cid:" + attachment.ContentId, fileInfo.FullName)
                            : body.Replace("cid:" + attachment.FileName, fileInfo.FullName);
                    }
                    else
                    {
                        // If we didn't find the cid tag we treat the inline attachment as a normal one 

                        files.Add(fileInfo.FullName);

                        if (htmlBody)
                        {
                            Console.WriteLine($"Attachment was marked as inline but the body did not contain the content id '{attachment.ContentId}' so mark it as a normal attachment");

                            if (hyperlinks == ReaderHyperLinks.Attachments || hyperlinks == ReaderHyperLinks.Both)
                                attachments.Add("<a href=\"" + fileInfo.Name + "\">" +
                                                WebUtility.HtmlEncode(attachmentFileName) + "</a> (" +
                                                FileManager.GetFileSizeString(fileInfo.Length) + ")");
                            else
                                attachments.Add(WebUtility.HtmlEncode(attachmentFileName) + " (" +
                                                FileManager.GetFileSizeString(fileInfo.Length) + ")");
                        }
                        else
                            attachments.Add(attachmentFileName + " (" + FileManager.GetFileSizeString(fileInfo.Length) + ")");
                    }

                    Console.WriteLine($"Attachment written to '{attachmentFileName}' with size '{FileManager.GetFileSizeString(attachment.Body.Length)}'");
                }
            }
            else
                Console.WriteLine("E-mail does not contain any attachments");

            Console.WriteLine("Stop pre processing EML stream");
        }
        #endregion

        #region GetErrorMessage
        /// <summary>
        /// Get the last know error message. When the string is empty there are no errors
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage()
        {
            return _errorMessage;
        }
        #endregion

        #region InjectHeader
        /// <summary>
        /// Inject an Outlook style header into the top of the html
        /// </summary>
        /// <param name="body"></param>
        /// <param name="header"></param>
        /// <param name="contentType">Content type</param>
        /// <returns></returns>
        private static string InjectHeader(string body, string header, string contentType = null)
        {
            Console.WriteLine("Start injecting header into body");

            var begin = body.IndexOf("<BODY", StringComparison.InvariantCultureIgnoreCase);

            if (begin <= 0) return header + body;
            begin = body.IndexOf(">", begin, StringComparison.InvariantCultureIgnoreCase);

            if (InjectHeaderAsIFrame)
            {
                header = "<style>iframe::-webkit-scrollbar {display: none;}</style>" +
                         "<iframe id=\"headerframe\" " +
                         " style=\"border:none; width:100%; margin-bottom:5px;\" " +
                         " onload='javascript:(function(o){o.style.height=o.contentWindow.document.body.scrollHeight+\"px\";}(this));' " +  // ensure height is correct
                         " srcdoc='" +
                         "<html style=\"overflow: hidden;\">" +
                         "     <body style=\"margin: 0;\">" + header + "</body>" +
                         "</html>" +
                         "'></iframe>";
            }

            body = body.Insert(begin + 1, header);

            /*if (!string.IsNullOrWhiteSpace(contentType))
            {
                // Inject content-type:
                var head = "<head";
                var headBegin = body.IndexOf(head, StringComparison.InvariantCultureIgnoreCase) + head.Length;
                headBegin = body.IndexOf(">", headBegin, StringComparison.InvariantCultureIgnoreCase);

                var contentHeader =
                    $"{Environment.NewLine}<meta http-equiv=\"Content-Type\" content=\"{contentType}\" charset=\"UTF-8\">{Environment.NewLine}";

                body = body.Insert(headBegin + 1, contentHeader);
            }*/
            var head = "<head";
            var headBegin = body.IndexOf(head, StringComparison.InvariantCultureIgnoreCase) + head.Length;
            headBegin = body.IndexOf(">", headBegin, StringComparison.InvariantCultureIgnoreCase);
            string scriptCode = @"
                            <script>
                            function toggle() {
                            if( document.getElementById('fields').style.display=='none' ){
                            document.getElementById('fields').style.display = 'block';
                            }else{
                            document.getElementById('fields').style.display = 'none';
                            }
                            }
                            function togglemeta() {
                            if( document.getElementById('meta').style.display=='none' ){
                            document.getElementById('meta').style.display = 'block';
                            }else{
                            document.getElementById('meta').style.display = 'none';
                            }
                            }
                             </script>";


            var contentHeader =
                $"{Environment.NewLine}<head><meta http-equiv=\"Content-Type\" content=\"{contentType}\" charset=\"UTF-8\">{scriptCode}</head>{Environment.NewLine}";

            body = body.Insert(headBegin + 1, contentHeader);

            Console.WriteLine("Stop injecting header into body");
            return body;
        }
        #endregion
    }

}
