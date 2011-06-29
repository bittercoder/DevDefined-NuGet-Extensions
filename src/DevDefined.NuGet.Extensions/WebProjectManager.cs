using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet;

namespace DevDefined.NuGet.Extensions
{
	public class WebProjectManager
	{
		readonly IProjectManager _projectManager;

		public WebProjectManager(string remoteSource, string siteRoot)
		{
			string webRepositoryDirectory = GetWebRepositoryDirectory(siteRoot);
			IPackageRepository sourceRepository = PackageRepositoryFactory.Default.CreateRepository(remoteSource);
			IPackagePathResolver pathResolver = new DefaultPackagePathResolver(webRepositoryDirectory);
			IPackageRepository localRepository = PackageRepositoryFactory.Default.CreateRepository(webRepositoryDirectory);
			IProjectSystem project = new WebProjectSystem(siteRoot);
			_projectManager = new ProjectManager(sourceRepository, pathResolver, project, localRepository);
		}

		public IPackageRepository LocalRepository
		{
			get { return _projectManager.LocalRepository; }
		}

		public IPackageRepository SourceRepository
		{
			get { return _projectManager.SourceRepository; }
		}

		public IQueryable<IPackage> GetInstalledPackages(string searchTerms)
		{
			return GetPackages(LocalRepository, searchTerms);
		}

		static IEnumerable<IPackage> GetPackageDependencies(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository)
		{
			IPackageRepository repository = localRepository;
			IPackageRepository repository2 = sourceRepository;
			ILogger instance = NullLogger.Instance;
			bool ignoreDependencies = false;
			var walker = new InstallWalker(repository, repository2, instance, ignoreDependencies);
			return
				walker.ResolveOperations(package).Where(delegate(PackageOperation operation) { return (operation.Action == PackageAction.Install); }).Select(
					delegate(PackageOperation operation) { return operation.Package; });
		}

		internal static IQueryable<IPackage> GetPackages(IQueryable<IPackage> packages, string searchTerm)
		{
			if (!string.IsNullOrEmpty(searchTerm))
			{
				searchTerm = searchTerm.Trim();
				packages = packages.Find(searchTerm.Split(new char[0]));
			}
			return packages;
		}

		internal static IQueryable<IPackage> GetPackages(IPackageRepository repository, string searchTerm)
		{
			return GetPackages(repository.GetPackages(), searchTerm);
		}

		internal IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package)
		{
			IPackageRepository localRepository = LocalRepository;
			IPackageRepository sourceRepository = SourceRepository;
			return GetPackagesRequiringLicenseAcceptance(package, localRepository, sourceRepository);
		}

		internal static IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository)
		{
			return GetPackageDependencies(package, localRepository, sourceRepository).Where(delegate(IPackage p) { return p.RequireLicenseAcceptance; });
		}

		public IQueryable<IPackage> GetPackagesWithUpdates(string searchTerms)
		{
			return GetPackages(LocalRepository.GetUpdates(SourceRepository.GetPackages()).AsQueryable(), searchTerms);
		}

		public IQueryable<IPackage> GetRemotePackages(string searchTerms)
		{
			return GetPackages(SourceRepository, searchTerms);
		}

		public IPackage GetUpdate(IPackage package)
		{
			return SourceRepository.GetUpdates(LocalRepository.GetPackages()).FirstOrDefault(delegate(IPackage p) { return (package.Id == p.Id); });
		}

		internal static string GetWebRepositoryDirectory(string siteRoot)
		{
			return Path.Combine(siteRoot, "App_Data", "packages");
		}

		public IEnumerable<string> InstallPackage(IPackage package)
		{
			return PerformLoggedAction(delegate
			                           	{
			                           		bool ignoreDependencies = false;
			                           		_projectManager.AddPackageReference(package.Id, package.Version, ignoreDependencies);
			                           	});
		}

		public bool IsPackageInstalled(IPackage package)
		{
			return LocalRepository.Exists(package);
		}

		IEnumerable<string> PerformLoggedAction(Action action)
		{
			var logger = new ErrorLogger();
			_projectManager.Logger = logger;
			try
			{
				action();
			}
			finally
			{
				_projectManager.Logger = null;
			}
			return logger.Errors;
		}

		public IEnumerable<string> UninstallPackage(IPackage package, bool removeDependencies)
		{
			return PerformLoggedAction(() =>
			                           	{
			                           		bool forceRemove = false;
			                           		bool flag1 = removeDependencies;
			                           		_projectManager.RemovePackageReference(package.Id, forceRemove, flag1);
			                           	});
		}

		public IEnumerable<string> UpdatePackage(IPackage package)
		{
			return PerformLoggedAction(() =>
			                           	{
			                           		bool updateDependencies = true;
			                           		_projectManager.UpdatePackageReference(package.Id, package.Version, updateDependencies);
			                           	});
		}

		#region Nested type: ErrorLogger

		class ErrorLogger : ILogger
		{
			readonly IList<string> _errors = new List<string>();

			public IEnumerable<string> Errors
			{
				get { return _errors; }
			}

			public void Log(MessageLevel level, string message, params object[] args)
			{
				if (level == MessageLevel.Warning)
				{
					_errors.Add(string.Format(CultureInfo.CurrentCulture, message, args));
				}
			}
		}

		#endregion
	}
}