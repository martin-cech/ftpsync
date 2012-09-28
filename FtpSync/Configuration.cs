using System.Linq;
using System.Collections.Generic;
using System;
using System.Xml.Linq;

namespace FtpSync
{
	public class Configuration
	{
		public string ServerRoot { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string LocalFolder { get; set; }

		public bool KeepNonexistingLocalFilesOnServer { get; set; }
		public bool KeepNonexistingLocalFoldersOnServer { get; set; }
		public bool UploadChangesOnly { get; set; }
		public bool IgnoreServerChanges { get; set; }
		public bool IgnoreInitialServerChanges { get; set; }
		public bool AskOnConflict { get; set; }

		public List<SyncFileInfo> SyncInfos { get; private set; }

		private readonly string _path;

		public Configuration(string path)
		{
			_path = path;
			SyncInfos = new List<SyncFileInfo>();
			NewSyncInfos = new List<SyncFileInfo>();
		}


		public List<SyncFileInfo> NewSyncInfos { get; private set; }


		public void UpdateLocalFile(SyncFileInfo syncFileInfo)
		{
			NewSyncInfos.Add(syncFileInfo);
		}

		public static Configuration LoadFromFile(string filename)
		{
			XElement doc = XDocument.Load(filename).Element("document");

			var cfgNode = doc.Element("config");

			var configuration = new Configuration(filename);

			configuration.KeepNonexistingLocalFilesOnServer = cfgNode.SafeParseBool("KeepNonexistingLocalFilesOnServer");
			configuration.KeepNonexistingLocalFoldersOnServer = cfgNode.SafeParseBool("KeepNonexistingLocalFoldersOnServer");
			configuration.UploadChangesOnly = cfgNode.SafeParseBool("UploadChangesOnly");
			configuration.IgnoreServerChanges = cfgNode.SafeParseBool("IgnoreServerChanges");
			configuration.IgnoreInitialServerChanges = cfgNode.SafeParseBool("IgnoreInitialServerChanges");
			configuration.AskOnConflict = cfgNode.SafeParseBool("AskOnConflict", true);

			configuration.Username = cfgNode.GetSonValue("Username");
			configuration.Password = cfgNode.GetSonValue("Password");
			configuration.LocalFolder = cfgNode.GetSonValue("LocalFolder");
			configuration.ServerRoot = cfgNode.GetSonValue("ServerRoot");

			var files = doc.Element("files").Elements("file").Select(
				e => new SyncFileInfo
				     	{
				     		FtpDetail = e.GetSonValue("ftpdetail"),
							LocalDetail = e.GetSonValue("localdetail"),
							LocalPath = e.GetSonValue("localpath"),
				     	});

			configuration.SyncInfos.AddRange(files);

			return configuration;
		}

		public void Save(string path = null)
		{
			var doc = new XDocument(
				new XElement("document",
				             new XElement("config",
				                          new XElement("KeepNonexistingLocalFilesOnServer", KeepNonexistingLocalFilesOnServer),
				                          new XElement("KeepNonexistingLocalFoldersOnServer", KeepNonexistingLocalFoldersOnServer),
				                          new XElement("UploadChangesOnly", UploadChangesOnly),
				                          new XElement("IgnoreServerChanges", IgnoreServerChanges),
				                          new XElement("IgnoreInitialServerChanges", IgnoreInitialServerChanges),
										  new XElement("AskOnConflict", AskOnConflict),

				                          new XElement("Username", Username),
				                          new XElement("Password", Password),
				                          new XElement("ServerRoot", ServerRoot),
				                          new XElement("LocalFolder", LocalFolder)
				             	),
				             new XElement("files",
				                          NewSyncInfos
				                          	.Select(i =>
				                          	        new XElement("file",
				                          	                     new XElement("localpath", i.LocalPath),
				                          	                     new XElement("localdetail", i.LocalDetail),
				                          	                     new XElement("ftpdetail", i.FtpDetail)))
				                          	.Cast<object>()
				                          	.ToArray())));

			doc.Save(path ?? _path);
		}
	}
}