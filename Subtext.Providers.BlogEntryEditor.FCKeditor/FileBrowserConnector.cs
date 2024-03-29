#region Disclaimer/Info

///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at Google Code at http://code.google.com/p/subtext/
// The development mailing list is at subtext@googlegroups.com 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////
///Part of this code derives from the original FileBrowserConnector shipped with 
///the original FredCK.FCKeditorV2, which is redistributed with the following license

#region Original License

/*
 * FCKeditor - The text editor for internet
 * Copyright (C) 2003-2005 Frederico Caldeira Knabben
 * 
 * Licensed under the terms of the GNU Lesser General Public License:
 * 		http://www.opensource.org/licenses/lgpl-license.php
 * 
 * For further information visit:
 * 		http://www.fckeditor.net/
 * 
 * "Support Open Source software. What about a donation today?"
 * 
 * 
 * File Authors:
 * 		Frederico Caldeira Knabben (fredck@fckeditor.net)
 */

#endregion

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using FredCK.FCKeditorV2;
using Subtext.Extensibility;
using Subtext.Extensibility.Interfaces;
using Subtext.Framework;
using Subtext.Framework.Components;
using Subtext.Framework.Configuration;
using Subtext.Framework.Providers;
using Subtext.Framework.Routing;
using Subtext.Framework.Web.Handlers;

namespace Subtext.Providers.BlogEntryEditor.FCKeditor
{
    /// <summary>
    /// Used to provide file management functionality for FCKEditor.
    /// </summary>
    public class FileBrowserConnector : SubtextPage
    {
        protected override void OnInit(EventArgs e)
        {
            if(!SubtextContext.User.IsInRole("Admins"))
            {
                SubtextContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                SubtextContext.HttpContext.Response.End();
            }
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            // Get the main request informaiton.
            string sCommand = Request.QueryString["Command"];
            if(sCommand == null)
            {
                return;
            }

            string sResourceType = Request.QueryString["Type"];
            if(sResourceType == null)
            {
                return;
            }

            string sCurrentFolder = Request.QueryString["CurrentFolder"];
            if(sCurrentFolder == null)
            {
                return;
            }

            // Check the current folder syntax (must begin and start with a slash).
            if(!sCurrentFolder.EndsWith("/"))
            {
                sCurrentFolder += "/";
            }
            if(!sCurrentFolder.StartsWith("/"))
            {
                sCurrentFolder = "/" + sCurrentFolder;
            }

            // File Upload doesn't have to return XML, so it must be intercepted before anything.
            if(sCommand == "FileUpload")
            {
                FileUpload(sResourceType, sCurrentFolder);
                return;
            }

            // Cleans the response buffer.
            Response.ClearHeaders();
            Response.Clear();

            // Prevent the browser from caching the result.
            Response.CacheControl = "no-cache";

            // Set the response format.
            Response.ContentEncoding = Encoding.UTF8;
            Response.ContentType = "text/xml";

            var oXML = new XmlDocument();
            XmlNode oConnectorNode = CreateBaseXml(oXML, sCommand, sResourceType, sCurrentFolder);

            if(CreateImageFolder(oConnectorNode))
            {
                if(sResourceType.Equals("Image"))
                {
                    // Execute the required command.
                    switch(sCommand)
                    {
                        case "GetFolders":
                            GetFolders(oConnectorNode, sCurrentFolder);
                            break;
                        case "GetFoldersAndFiles":
                            GetFolders(oConnectorNode, sCurrentFolder);
                            GetFiles(oConnectorNode, sResourceType, sCurrentFolder);
                            break;
                        case "CreateFolder":
                            CreateFolder(oConnectorNode, sResourceType, sCurrentFolder);
                            break;
                    }
                }
                else if(sResourceType.Equals("Posts"))
                {
                    // Execute the required command.
                    switch(sCommand)
                    {
                        case "GetFolders":
                            GetCategories(oConnectorNode, sCurrentFolder);
                            break;
                        case "GetFoldersAndFiles":
                            GetCategories(oConnectorNode, sCurrentFolder);
                            GetPosts(oConnectorNode, sCurrentFolder);
                            break;
                        case "CreateFolder":
                            CreateFolder(oConnectorNode, sResourceType, sCurrentFolder);
                            break;
                    }
                }
                else if(sResourceType.Equals("File"))
                {
                    // Execute the required command.
                    switch(sCommand)
                    {
                        case "GetFolders":
                            GetFolders(oConnectorNode, sCurrentFolder);
                            break;
                        case "GetFoldersAndFiles":
                            GetFolders(oConnectorNode, sCurrentFolder);
                            GetFiles(oConnectorNode, sResourceType, sCurrentFolder);
                            break;
                        case "CreateFolder":
                            CreateFolder(oConnectorNode, sResourceType, sCurrentFolder);
                            break;
                    }
                }
            }

            // Output the resulting XML.
            Response.Write(oXML.OuterXml);

            Response.End();

            base.OnLoad(e);
        }

        private void GetFolders(XmlNode connectorNode, string currentFolder)
        {
            // Map the virtual path to the local server path.
            string sServerDir = ServerMapFolder(currentFolder, Url.ImageDirectoryUrl(Blog));

            // Create the "Folders" node.
            XmlNode oFoldersNode = XmlUtil.AppendElement(connectorNode, "Folders");

            var oDir = new DirectoryInfo(sServerDir);
            DirectoryInfo[] aSubDirs = oDir.GetDirectories();

            for(int i = 0; i < aSubDirs.Length; i++)
            {
                // Create the "Folders" node.
                XmlNode oFolderNode = XmlUtil.AppendElement(oFoldersNode, "Folder");
                XmlUtil.SetAttribute(oFolderNode, "name", aSubDirs[i].Name);
            }
        }

        private void GetFiles(XmlNode connectorNode, string resourceType, string currentFolder)
        {
            // Map the virtual path to the local server path.
            string sServerDir = ServerMapFolder(currentFolder, Url.ImageDirectoryUrl(Blog));

            // Create the "Files" node.
            XmlNode oFilesNode = XmlUtil.AppendElement(connectorNode, "Files");

            var oDir = new DirectoryInfo(sServerDir);
            FileInfo[] aFiles = oDir.GetFiles();

            for(int i = 0; i < aFiles.Length; i++)
            {
                if(Regex.IsMatch(aFiles[i].Extension, GetAllowedExtension(resourceType), RegexOptions.IgnoreCase))
                {
                    Decimal iFileSize = Math.Round((Decimal)aFiles[i].Length / 1024);
                    if(iFileSize < 1 && aFiles[i].Length != 0)
                    {
                        iFileSize = 1;
                    }

                    // Create the "File" node.
                    XmlNode oFileNode = XmlUtil.AppendElement(oFilesNode, "File");
                    XmlUtil.SetAttribute(oFileNode, "name", aFiles[i].Name);
                    XmlUtil.SetAttribute(oFileNode, "size", iFileSize.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification =
                "The error number is used to create an error node in the XML document, so we need to catch a general exception as well."
            )]
        private void CreateFolder(XmlNode connectorNode, string resourceType, string currentFolder)
        {
            string sErrorNumber = "0";

            if(resourceType.Equals("Posts"))
            {
                sErrorNumber = "103";
            }
            else
            {
                string sNewFolderName = Request.QueryString["NewFolderName"];

                if(sNewFolderName == null || sNewFolderName.Length == 0)
                {
                    sErrorNumber = "102";
                }
                else
                {
                    // Map the virtual path to the local server path of the current folder.
                    string sServerDir = ServerMapFolder(currentFolder, Url.ImageDirectoryUrl(Blog));

                    try
                    {
                        Util.CreateDirectory(Path.Combine(sServerDir, sNewFolderName));
                    }
                    catch(ArgumentException)
                    {
                        sErrorNumber = "102";
                    }
                    catch(PathTooLongException)
                    {
                        sErrorNumber = "102";
                    }
                    catch(IOException)
                    {
                        sErrorNumber = "101";
                    }
                    catch(SecurityException)
                    {
                        sErrorNumber = "103";
                    }
                    catch(Exception)
                    {
                        sErrorNumber = "110";
                    }
                }
            }

            // Create the "Error" node.
            XmlNode oErrorNode = XmlUtil.AppendElement(connectorNode, "Error");
            XmlUtil.SetAttribute(oErrorNode, "number", sErrorNumber);
        }

        private void FileUpload(string resourceType, string currentFolder)
        {
            string sErrorNumber = "0";
            string sFileName = "";

            if(!resourceType.Equals("Posts"))
            {
                HttpPostedFile oFile = Request.Files["NewFile"];

                if(oFile != null)
                {
                    // Map the virtual path to the local server path.
                    string sServerDir = ServerMapFolder(currentFolder, Url.ImageDirectoryUrl(Blog));

                    // Get the uploaded file name.
                    sFileName = Path.GetFileName(oFile.FileName);

                    int iCounter = 0;

                    while(true)
                    {
                        string sFilePath = Path.Combine(sServerDir, sFileName);

                        if(File.Exists(sFilePath))
                        {
                            iCounter++;
                            sFileName =
                                Path.GetFileNameWithoutExtension(oFile.FileName)
                                + "(" + iCounter + ")"
                                + Path.GetExtension(oFile.FileName);

                            sErrorNumber = "201";
                        }
                        else
                        {
                            oFile.SaveAs(sFilePath);
                            break;
                        }
                    }
                }
                else
                {
                    sErrorNumber = "202";
                }
            }
            else
            {
                sErrorNumber = "203";
            }

            Response.Clear();
            Response.Write("<script type=\"text/javascript\">");
            Response.Write("window.parent.frames['frmUpload'].OnUploadCompleted(" + sErrorNumber + ",'" +
                           sFileName.Replace("'", "\\'") + "') ;");
            Response.Write("</script>");

            Response.End();
        }

        private XmlNode CreateBaseXml(XmlDocument xml, string command, string resourceType, string currentFolder)
        {
            // Create the XML document header.
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));

            // Create the main "Connector" node.
            XmlNode connectorNode = XmlUtil.AppendElement(xml, "Connector");
            XmlUtil.SetAttribute(connectorNode, "command", command);
            XmlUtil.SetAttribute(connectorNode, "resourceType", resourceType);

            // Add the current folder node.
            if(!resourceType.Equals("Posts"))
            {
                XmlNode oCurrentNode = XmlUtil.AppendElement(connectorNode, "CurrentFolder");
                XmlUtil.SetAttribute(oCurrentNode, "path", currentFolder);
                XmlUtil.SetAttribute(oCurrentNode, "url", Url.ImageDirectoryUrl(Blog) + currentFolder);
            }
            else
            {
                XmlNode currentNode = XmlUtil.AppendElement(connectorNode, "CurrentFolder");
                XmlUtil.SetAttribute(currentNode, "path", currentFolder);
                XmlUtil.SetAttribute(currentNode, "url", "");
            }

            return connectorNode;
        }

        private string ServerMapFolder(string folderPath, string imageDirectoryUrl)
        {
            // Get the resource type directory.
            string imageDirectoryPath = Server.MapPath(imageDirectoryUrl);

            // Return the resource type directory combined with the required path.
            return Path.Combine(imageDirectoryPath, folderPath.TrimStart('/'));
        }

        private static string GetUrlFromPath(string folderPath, string imageDirectoryUrl)
        {
            return imageDirectoryUrl + folderPath.Substring(1);
        }

        private static string GetAllowedExtension(string resourceType)
        {
            string extStr = string.Empty;
            if(resourceType.Equals("File"))
            {
                extStr = FckBlogEntryEditorProvider.FileAllowedExtensions;
            }
            else if(resourceType.Equals("Image"))
            {
                extStr = FckBlogEntryEditorProvider.ImageAllowedExtensions;
            }
            return extStr;
        }

        private bool CreateImageFolder(XmlNode connectorNode)
        {
            bool retval;
            string imageDirectoryPath = Url.ImageDirectoryPath(Blog);
            try
            {
                if(!Directory.Exists(imageDirectoryPath))
                {
                    Directory.CreateDirectory(imageDirectoryPath);
                }
                retval = true;
            }
            catch(Exception)
            {
                // Create the "Error" node.
                XmlNode errorNode = XmlUtil.AppendElement(connectorNode, "Error");
                XmlUtil.SetAttribute(errorNode, "number", "1");
                XmlUtil.SetAttribute(errorNode, "text",
                                     "Cannot create folder: " + imageDirectoryPath + "." + Environment.NewLine +
                                     "Write access to this folder is required to initialize the image storage");
                retval = false;
            }
            return retval;
        }

        #region Post Type Handler

        private static void GetCategories(XmlNode connectorNode, string currentFolder)
        {
            if(currentFolder.Equals("/"))
            {
                ICollection<LinkCategory> catList = Links.GetCategories(CategoryType.PostCollection, ActiveFilter.None);

                // Create the "Folders" node.
                XmlNode oFoldersNode = XmlUtil.AppendElement(connectorNode, "Folders");

                foreach(LinkCategory category in catList)
                {
                    // Create the "Folders" node.
                    XmlNode oFolderNode = XmlUtil.AppendElement(oFoldersNode, "Folder");
                    XmlUtil.SetAttribute(oFolderNode, "name", category.Title);
                }
            }
        }

        private void GetPosts(XmlNode connectorNode, string currentFolder)
        {
            IPagedCollection<EntryStatsView> posts;
            if(currentFolder.Equals("/"))
            {
                posts = Repository.GetEntries(PostType.BlogPost, -1, 0, 1000);
            }
            else
            {
                string categoryName = currentFolder.Substring(1, currentFolder.Length - 2);
                LinkCategory cat = ObjectProvider.Instance().GetLinkCategory(categoryName, false);
                posts = Repository.GetEntries(PostType.BlogPost, cat.Id, 0, 1000);
            }

            // Create the "Files" node.
            XmlNode oFilesNode = XmlUtil.AppendElement(connectorNode, "Files");
            foreach(var entry in posts)
            {
                // Create the "File" node.
                if(entry.IsActive)
                {
                    XmlNode oFileNode = XmlUtil.AppendElement(oFilesNode, "File");

                    //TODO: Seriously refactor.
                    var urlHelper = new UrlHelper(null, null);

                    XmlUtil.SetAttribute(oFileNode, "name",
                                         string.Format(CultureInfo.InvariantCulture, "{0}|{1}", entry.Title,
                                                       urlHelper.EntryUrl(entry).ToFullyQualifiedUrl(Config.CurrentBlog)));
                    XmlUtil.SetAttribute(oFileNode, "size", entry.DateModified.ToShortDateString());
                }
            }
        }

        #endregion
    }
}