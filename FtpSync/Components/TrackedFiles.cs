using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Xml.Linq;
using FtpSync.Helpers;
using FtpSync.Utils;

namespace FtpSync.Components
{
	public class TrackedFiles
	{
		public SyncFiles Files { get; private set; }
		private readonly HashSet<SyncFileInfo> _newfiles = new HashSet<SyncFileInfo>();

		private readonly SyncDirectories _oldDirectories = new SyncDirectories();


		private readonly SyncDirectories _newDirectories = new SyncDirectories();

		private readonly string _path;

		private TrackedFiles(string path)
		{
			_path = path;
			Files = new SyncFiles();
		}

		public static TrackedFiles TryLoad(string path)
		{
			return File.Exists(path)
			       	? Load(path)
			       	: new TrackedFiles(path);
		}

		public static TrackedFiles Load(string path)
		{
			var trackedFiles = new TrackedFiles(path);
			XElement doc = XDocument.Load(path).Element("document");

			doc.Element("files").Elements("file").Select(
				e => new SyncFileInfo
				     	{
				     		FtpDetail = e.GetSonValue("ftpdetail"),
				     		LocalDetail = e.GetSonValue("localdetail"),
				     		LocalPath = e.GetSonValue("localpath"),
				     	})
				.ToList()
				.ForEach(f => trackedFiles.Files.Add(f));

			doc.Element("directories").Elements("directory")
				.Select(e => new SyncDirInfo
				             	{
				             		FtpPath = e.GetSonValue("ftppath"),
				             		LocalPath = e.GetSonValue("localpath")
				             	})
				.ToList()
				.ForEach(d => trackedFiles._oldDirectories.Add(d));

			return trackedFiles;

		}

		public void Save(string path = null)
		{
			var doc = new XDocument(
				new XElement("document",
				             new XElement("files",
				                          _newfiles
				                          	.Select(i =>
				                          	        new XElement("file",
				                          	                     new XElement("localpath", i.LocalPath),
				                          	                     new XElement("localdetail", i.LocalDetail),
				                          	                     new XElement("ftpdetail", i.FtpDetail)))
				                          	.Cast<object>()
				                          	.ToArray()),
				             new XElement("directories",
				                          _newDirectories
										  .GetAll()
				                          	.Select(i =>
				                          	        new XElement("directory",
				                          	                     new XElement("localpath", i.LocalPath),
				                          	                     new XElement("ftppath", i.FtpPath)))
				                          	.Cast<object>()
				                          	.ToArray())));

			doc.Save(path ?? _path);
		}

		public bool IsDirectoryTracked(string ftpPath)
		{
			return _oldDirectories.ByFtpPath(ftpPath) != null;
		}

		public void TrackDirectory(string localPath, string ftpPath)
		{
			if (_newDirectories.ByLocalPath(localPath) != null) return;
			_newDirectories.Add(new SyncDirInfo {FtpPath = ftpPath, LocalPath = localPath});
		}

		public void TrackFile(SyncFileInfo syncFileInfo)
		{
			_newfiles.Add(syncFileInfo);
		}

		public static TrackedFiles Create(string path)
		{
			return new TrackedFiles(path);
		}
	}
}