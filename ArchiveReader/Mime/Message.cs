using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using MsgReader.Helpers;
using MsgReader.Mime.Header;
using MsgReader.Mime.Traverse;


// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable IntroduceOptionalParameters.Global

namespace MsgReader.Mime
{
	/// <summary>
	/// This is the root of the email tree structure.<br/>
	/// <see cref="Mime.MessagePart"/> for a description about the structure.<br/>
	/// <br/>
	/// A Message (this class) contains the headers of an email message such as:
	/// <code>
	///  - To
	///  - From
	///  - Subject
	///  - Content-Type
	///  - Message-ID
	/// </code>
	/// which are located in the <see cref="Headers"/> property.<br/>
	/// <br/>
	/// Use the <see cref="Message.MessagePart"/> property to find the actual content of the email message.
	/// </summary>
	public class Message
	{
		#region Properties
        /// <summary>
        /// Returns the ID of the message when this is available in the <see cref="Headers"/>
        /// (as specified in [RFC2822]). Null when not available
        /// </summary>
        public string Id
        {
            get
            {
                if (Headers == null)
                    return null;

                return !string.IsNullOrEmpty(Headers.MessageId) ? Headers.MessageId : null;
            }
        }

		/// <summary>
		/// Headers of the Message.
		/// </summary>
		public MessageHeader Headers { get; }

		/// <summary>
		/// This is the body of the email Message.<br/>
		/// <br/>
		/// If the body was parsed for this Message, this property will never be <see langword="null"/>.
		/// </summary>
		public MessagePart MessagePart { get; }

        /// <summary>
        /// This will return the first <see cref="MessagePart"/> where the <see cref="ContentType.MediaType"/>
        /// is set to "html/text". This will return <see langword="null"/> when there is no "html/text" 
        /// <see cref="MessagePart"/> found.
        /// </summary>
        public MessagePart HtmlBody { get; private set; }

        /// <summary>
        /// This will return the first <see cref="MessagePart"/> where the <see cref="ContentType.MediaType"/>
        /// is set to "text/plain". This will be <see langword="null"/> when there is no "text/plain" 
        /// <see cref="MessagePart"/> found.
        /// </summary>
        public MessagePart TextBody { get; private set; }

        /// <summary>
        /// This will return all the <see cref="MessagePart">message parts</see> that are flagged as 
        /// <see cref="Mime.MessagePart.IsAttachment"/>.
        /// This will be <see langword="null"/> when there are no <see cref="MessagePart">message parts</see> 
        /// that are flagged as <see cref="Mime.MessagePart.IsAttachment"/>.
        /// </summary>
        public ReadOnlyCollection<MessagePart> Attachments { get; } 

        /// <summary>
		/// The raw content from which this message has been constructed.<br/>
		/// These bytes can be persisted and later used to recreate the Message.
		/// </summary>
		public byte[] RawMessage { get; }

        /// <summary>
        /// Returns <c>trye</c> when the signature is valid />
        /// </summary>
        public bool? SignatureIsValid { get; private set; }

        /// <summary>
        /// Returns the name of the person who signed the message 
        /// </summary>
        public string SignedBy { get; private set; }

        /// <summary>
        /// Returns the date and time when the message has been signed
        /// </summary>
        public DateTime? SignedOn { get; private set; }

        /// <summary>
        /// Returns the certificate that has been used to sign the message
        /// </summary>
        public X509Certificate2 SignedCertificate { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Convenience constructor for <see cref="Mime.Message(byte[], bool)"/>.<br/>
		/// <br/>
		/// Creates a message from a byte array. The full message including its body is parsed.
		/// </summary>
		/// <param name="rawMessageContent">The byte array which is the message contents to parse</param>
		public Message(byte[] rawMessageContent) : this(rawMessageContent, true) {}
        public Dictionary<string, string> NotesItems = new Dictionary<string, string>();
        /// <summary>
        /// Constructs a message from a byte array.<br/>
        /// <br/>
        /// The headers are always parsed, but if <paramref name="parseBody"/> is <see langword="false"/>, the body is not parsed.
        /// </summary>
        /// <param name="rawMessageContent">The byte array which is the message contents to parse</param>
        /// <param name="parseBody">
        /// <see langword="true"/> if the body should be parsed,
        /// <see langword="false"/> if only headers should be parsed out of the <paramref name="rawMessageContent"/> byte array
        /// </param>
        //private Configuration configuration = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

        public Message(byte[] rawMessageContent, bool parseBody)
		{
            Logger.WriteToLog("Processing raw EML message content");

			RawMessage = rawMessageContent;

			// Find the headers and the body parts of the byte array
		    HeaderExtractor.ExtractHeadersAndBody(rawMessageContent, out var headersTemp, out var body);

			// Set the Headers property
			Headers = headersTemp;

			// Should we also parse the body?
			if (parseBody)
			{
				// Parse the body into a MessagePart
				MessagePart = new MessagePart(body, Headers);

                var attachments = new AttachmentFinder().VisitMessage(this);

                if (MessagePart.ContentType?.MediaType == "multipart/signed")
                {
                    foreach (var attachment in attachments)
                    {
                        if (attachment.FileName.ToUpperInvariant() == "SMIME.P7S")
                            ProcessSignedContent(attachment.Body);
                    }
                }
                /*if (MessagePart.NotesItems != null) 
                {
                    string tmpFile = CreateTmpFile();
                    Console.WriteLine($"here is the {tmpFile}");
                    
                    NotesItems = MessagePart.NotesItems;
                    try
                    {
                        StreamWriter streamWriter = File.AppendText(tmpFile);
                        string html = "<!DOCTYPE html><html lang = \"en\"><head><meta http-equiv=\"content-type\" content=\"text/html\" /></head><table>";
                        foreach (KeyValuePair<string, string> pair in NotesItems)
                        {
                            string tmp = pair.Value.ToString();
                            Console.WriteLine(tmp);
                            Console.WriteLine(pair.Key.ToString());
                            if (tmp.StartsWith("=?ISO-8859-1"))
                            {
                                tmp = DecodeQuotedPrintables(tmp, "UTF-8");                            
                                tmp = tmp.Replace("=?ISO-8859-1?Q?", "");
                                tmp = tmp.Replace("?=", "");
                            }
                            

                            html = html + String.Format("<tr><td>{0}</td><td>{1}</td></tr>", pair.Key, tmp);
                        }
                        html = html + "</table></html>";
                        //streamWriter.Write(html);
                        streamWriter.Flush();
                        streamWriter.Close();
                        Console.WriteLine($"TEMP file updated.{html}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing to TEMP file:{ex.Message}");
                    }

                }*/

			    var findBodyMessagePartWithMediaType = new FindBodyMessagePartWithMediaType();

                // Searches for the first HTML body and mark this one as the HTML body of the E-mail
                if (HtmlBody == null)
                {
                    HtmlBody = findBodyMessagePartWithMediaType.VisitMessage(this, "text/html");
                    if (HtmlBody != null)
                        HtmlBody.IsHtmlBody = true;
                }

                // Searches for the first TEXT body and mark this one as the TEXT body of the E-mail
                if (TextBody == null)
                {
                    TextBody = findBodyMessagePartWithMediaType.VisitMessage(this, "text/plain");
                    if (TextBody != null)
                        TextBody.IsTextBody = true;
                }
               

                if (HtmlBody != null)
			    {
			        foreach (var attachment in attachments)
			        {
			            if (attachment.IsInline || attachment.ContentId == null || attachment.FileName.ToUpperInvariant() == "SMIME.P7S" )
			                continue;

                        var htmlBody = HtmlBody.BodyEncoding.GetString(HtmlBody.Body);
			            attachment.IsInline = htmlBody.Contains($"cid:{attachment.ContentId}");
			        }
			    }

                if (attachments != null)
                {
                    var result = new List<MessagePart>();
                    foreach (var attachment in attachments)
                    {
                        if (attachment.FileName.ToUpperInvariant() == "SMIME.P7S" ||
                            attachment.IsHtmlBody ||
                            attachment.IsTextBody)
                            continue;

                        result.Add(attachment);
                    }

                    Attachments = result.AsReadOnly();
                }
                
            }

            Logger.WriteToLog("Raw EML message content processed");
		}
        #endregion
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

        #region ProcessSignedContent
        /// <summary>
        /// Processes the signed content
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private void ProcessSignedContent(byte[] data)
        {
            Logger.WriteToLog("Processing signed content");

            var signedCms = new SignedCms();
            signedCms.Decode(data);

            try
            {
                //signedCms.CheckSignature(signedCms.Certificates, false);
                foreach (var cert in signedCms.Certificates)
                    SignatureIsValid = cert.Verify();

                SignatureIsValid = true;
                foreach (var cryptographicAttributeObject in signedCms.SignerInfos[0].SignedAttributes)
                {
                    if (cryptographicAttributeObject.Values[0] is Pkcs9SigningTime pkcs9SigningTime)
                        SignedOn = pkcs9SigningTime.SigningTime.ToLocalTime();
                }

                var certificate = signedCms.SignerInfos[0].Certificate;
                if (certificate != null)
                {
                    SignedCertificate = certificate;
                    SignedBy = certificate.GetNameInfo(X509NameType.SimpleName, false);
                }
            }
            catch (CryptographicException)
            {
                SignatureIsValid = false;
            }

            // Get the decoded attachment
            using (var memoryStream = StreamHelpers.Manager.GetStream("Message.cs", signedCms.ContentInfo.Content, 0, signedCms.ContentInfo.Content.Length))
            {
                var eml = Load(memoryStream);
                if (eml.TextBody != null)
                    TextBody = eml.TextBody;

                if (eml.HtmlBody != null)
                    HtmlBody = eml.HtmlBody;

                //foreach (var emlAttachment in eml.Attachments)
            }

            Logger.WriteToLog("Signed content processed");
        }
        #endregion

        #region GetEmailAddresses
        /// <summary>
        /// Returns the list of <see cref="RfcMailAddress"/> as a normal or html string
        /// </summary>
        /// <param name="rfcMailAddresses">A list with one or more <see cref="RfcMailAddress"/> objects</param>
        /// <param name="convertToHref">When true the E-mail addresses are converted to hyperlinks</param>
        /// <param name="html">Set this to true when the E-mail body format is html</param>
        /// <returns></returns>
        public string GetEmailAddresses(IEnumerable<RfcMailAddress> rfcMailAddresses, bool convertToHref, bool html)
        {
            Logger.WriteToLog("Getting mail addresses");

            var result = string.Empty;

            if (rfcMailAddresses == null)
                return result;

            foreach (var rfcMailAddress in rfcMailAddresses)
            {
                if (result != string.Empty)
                    result += "; ";

                var emailAddress = string.Empty;
                var displayName = rfcMailAddress.DisplayName;

                if (rfcMailAddress.HasValidMailAddress)
                    emailAddress = rfcMailAddress.Address;

                if (string.Equals(emailAddress, displayName, StringComparison.InvariantCultureIgnoreCase))
                    displayName = string.Empty;

                if (html)
                {
                    emailAddress = WebUtility.HtmlEncode(emailAddress);
                    displayName = WebUtility.HtmlEncode(displayName);
                }

                if (convertToHref && html && !string.IsNullOrEmpty(emailAddress))
                    result += "<a href=\"mailto:" + emailAddress + "\">" +
                              (!string.IsNullOrEmpty(displayName)
                                  ? displayName
                                  : emailAddress) + "</a>";

                else
                {
                    if (!string.IsNullOrEmpty(displayName))
                        result += displayName;

                    var beginTag = string.Empty;
                    var endTag = string.Empty;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        if (html)
                        {
                            beginTag = "&nbsp;&lt;";
                            endTag = "&gt;";
                        }
                        else
                        {
                            beginTag = " <";
                            endTag = ">";
                        }
                    }

                    if (!string.IsNullOrEmpty(emailAddress))
                        result += beginTag + emailAddress + endTag;
                }
            }

            return result;
        }
        #endregion

        #region Save
		/// <summary>
		/// Save this <see cref="Message"/> to a file.<br/>
		/// <br/>
		/// Can be loaded at a later time using the <see cref="Load(FileInfo)"/> method.
		/// </summary>
		/// <param name="file">The File location to save the <see cref="Message"/> to. Existent files will be overwritten.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="file"/> is <see langword="null"/></exception>
		/// <exception>Other exceptions relevant to using a <see cref="FileStream"/> might be thrown as well</exception>
		// ReSharper disable once UnusedMember.Global
		public void Save(FileInfo file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			using (var fileStream = new FileStream(file.FullName, FileMode.Create))
				Save(fileStream);
		}

		/// <summary>
		/// Save this <see cref="Message"/> to a stream.<br/>
		/// </summary>
		/// <param name="messageStream">The stream to write to</param>
		/// <exception cref="ArgumentNullException">If <paramref name="messageStream"/> is <see langword="null"/></exception>
		/// <exception>Other exceptions relevant to <see cref="Stream.Write"/> might be thrown as well</exception>
		public void Save(Stream messageStream)
		{
			if (messageStream == null)
				throw new ArgumentNullException(nameof(messageStream));

			messageStream.Write(RawMessage, 0, RawMessage.Length);
		}
        #endregion

        #region Load
        /// <summary>
		/// Loads a <see cref="Message"/> from a file containing a raw email.
		/// </summary>
		/// <param name="file">The File location to load the <see cref="Message"/> from. The file must exist.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="file"/> is <see langword="null"/></exception>
		/// <exception cref="FileNotFoundException">If <paramref name="file"/> does not exist</exception>
		/// <exception>Other exceptions relevant to a <see cref="FileStream"/> might be thrown as well</exception>
		/// <returns>A <see cref="Message"/> with the content loaded from the <paramref name="file"/></returns>
		public static Message Load(FileInfo file)
		{
            Logger.WriteToLog($"Loading EML file from '{file.FullName}'");
            Console.WriteLine(file.FullName);
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			if (!file.Exists)
				throw new FileNotFoundException("Cannot load message from non-existent file", file.FullName);

			using (var fileStream = new FileStream(file.FullName, FileMode.Open))
                return Load(fileStream);
		}

		/// <summary>
		/// Loads a <see cref="Message"/> from a <see cref="Stream"/> containing a raw email.
		/// </summary>
		/// <param name="messageStream">The <see cref="Stream"/> from which to load the raw <see cref="Message"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="messageStream"/> is <see langword="null"/></exception>
		/// <exception>Other exceptions relevant to <see cref="Stream.Read"/> might be thrown as well</exception>
		/// <returns>A <see cref="Message"/> with the content loaded from the <paramref name="messageStream"/></returns>
		public static Message Load(Stream messageStream)
		{
		    Logger.WriteToLog("Loading EML file from stream");
            
            if (messageStream == null)
				throw new ArgumentNullException(nameof(messageStream));

			using (var memoryStream = StreamHelpers.Manager.GetStream())
			{
                messageStream.CopyTo(memoryStream);
                var content = memoryStream.ToArray();

                StreamReader reader = new StreamReader(messageStream);
                string text = reader.ReadToEnd();              
                //Console.WriteLine($"Stream {text}");

                return new Message(content);
			}
		}
        #endregion
        private static string CreateTmpFile()
        {
            string oldfileName = Path.GetTempPath() + "NotesHeader.htm";
            DeleteTmpFile(oldfileName);
            string fileName = string.Empty;

            try
            {
                // Get the full name of the newly created Temporary file. 
                // Note that the GetTempFileName() method actually creates
                // a 0-byte file and returns the name of the created file.
                fileName = Path.GetTempPath() + "NotesHeader.htm";

                // Craete a FileInfo object to set the file's attributes
                FileInfo fileInfo = new FileInfo(fileName);

                // Set the Attribute property of this file to Temporary. 
                // Although this is not completely necessary, the .NET Framework is able 
                // to optimize the use of Temporary files by keeping them cached in memory.
                fileInfo.Attributes = FileAttributes.Temporary;
                
                Console.WriteLine("TEMP file created at: " + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create TEMP file or set its attributes: " + ex.Message);
            }

            return fileName;
        }
        private static void DeleteTmpFile(string tmpFile)
        {
            try
            {
                // Delete the temp file (if it exists)
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                    Console.WriteLine("TEMP file deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleteing TEMP file: " + ex.Message);
            }
        }


    }
    public static class StringExtensions
    {
        public static string ToUTF8(this string text)
        {
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(text));
        }
    }
}
