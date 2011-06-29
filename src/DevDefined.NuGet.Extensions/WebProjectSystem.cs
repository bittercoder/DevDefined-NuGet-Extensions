using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using NuGet;

namespace DevDefined.NuGet.Extensions
{
	public class WebProjectSystem : PhysicalFileSystem, IProjectSystem
	{
		const string BinDir = "bin";

		public WebProjectSystem(string root)
			: base(root)
		{
		}

		public void AddReference(string referencePath, Stream stream)
		{
			string fileName = Path.GetFileName(referencePath);
			string fullPath = GetFullPath(GetReferencePath(fileName));
			AddFile(fullPath, stream);
		}

		public void AddFrameworkReference(string name)
		{
			// No-op
		}

		public dynamic GetPropertyValue(string propertyName)
		{
			if ((propertyName != null) && propertyName.Equals("RootNamespace", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}

			return null;
		}

		public bool IsSupportedFile(string path)
		{
			return (!path.StartsWith("tools", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(path).Equals("app.config", StringComparison.OrdinalIgnoreCase));
		}

		public virtual string ResolvePath(string path)
		{
			return path;
		}

		public bool ReferenceExists(string name)
		{
			string referencePath = GetReferencePath(name);
			return FileExists(referencePath);
		}

		public void RemoveReference(string name)
		{
			DeleteFile(GetReferencePath(name));
			if (!GetFiles("bin").Any())
			{
				DeleteDirectory("bin");
			}
		}

		public string ProjectName
		{
			get { return base.Root; }
		}

		public FrameworkName TargetFramework
		{
			get { return VersionUtility.DefaultTargetFramework; }
		}

		protected virtual string GetReferencePath(string name)
		{
			return Path.Combine("bin", name);
		}
	}
}