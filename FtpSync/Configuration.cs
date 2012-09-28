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

		public List<SyncFileInfo> SyncInfos { get; private set; }

		public Configuration()
		{
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

			var cfg = doc.Element("config");

			var config = new Configuration();

			config.KeepNonexistingLocalFilesOnServer = bool.Parse(cfg.Element("KeepNonexistingLocalFilesOnServer").Value);
			config.KeepNonexistingLocalFoldersOnServer = bool.Parse(cfg.Element("KeepNonexistingLocalFoldersOnServer").Value);
			config.UploadChangesOnly = bool.Parse(cfg.Element("UploadChangesOnly").Value);
			config.IgnoreServerChanges = bool.Parse(cfg.Element("IgnoreServerChanges").Value);
			config.IgnoreInitialServerChanges = bool.Parse(cfg.Element("IgnoreInitialServerChanges").Value);

			config.Username = cfg.Element("Username").Value;
			config.Password = cfg.Element("Password").Value;
			config.LocalFolder = cfg.Element("LocalFolder").Value;
			config.ServerRoot = cfg.Element("ServerRoot").Value;

			var files = doc.Element("files").Elements("file").Select(
				e => new SyncFileInfo
				     	{
				     		FtpInfo = e.Element("ftpinfo").Value,
				     		LocalInfo = e.Element("localinfo").Value,
				     		LocalPath = e.Element("localpath").Value,
				     	});

			config.SyncInfos.AddRange(files);

			return config;
		}

		public void SaveToFile(string filename)
		{
			var doc = new XDocument(
				new XElement("document",
				             new XElement("config",
				                          new XElement("KeepNonexistingLocalFilesOnServer", KeepNonexistingLocalFilesOnServer),
				                          new XElement("KeepNonexistingLocalFoldersOnServer", KeepNonexistingLocalFoldersOnServer),
				                          new XElement("UploadChangesOnly", UploadChangesOnly),
				                          new XElement("IgnoreServerChanges", IgnoreServerChanges),
				                          new XElement("IgnoreInitialServerChanges", IgnoreInitialServerChanges),

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
				                          	                     new XElement("localinfo", i.LocalInfo),
				                          	                     new XElement("ftpinfo", i.FtpInfo)))
				                          	.Cast<object>()
				                          	.ToArray())));

			doc.Save(filename);
		}
	}
}