public static T WithRawArgument<T>(this T settings, string rawArgument) where T : Cake.Core.Tooling.ToolSettings
{
    if (settings == null) throw new ArgumentNullException(nameof(settings));

    if (!string.IsNullOrEmpty(rawArgument))
    {
        var previousCustomizer = settings.ArgumentCustomization;
        if (previousCustomizer != null)
            settings.ArgumentCustomization = builder => previousCustomizer.Invoke(builder).Append(rawArgument);
        else
            settings.ArgumentCustomization = builder => builder.Append(rawArgument);
    }

    return settings;
}

void DeleteDirectoryRobust(params string[] directories)
{
    DeleteDirectoryRobust(Context, directories);
}

static void DeleteDirectoryRobust(this ICakeContext context, params DirectoryPath[] directories)
{
    DeleteDirectoryRobust(context, Array.ConvertAll(directories, d => d.FullPath));
}

static void DeleteDirectoryRobust(this ICakeContext context, params string[] directories)
{
    if (!directories.Any()) return;

    context.Information("Deleting directories:");

    foreach (var directory in directories)
    {
        for (var attempt = 1;; attempt++)
        {
            context.Information(directory);
            try
            {
                System.IO.Directory.Delete(directory, recursive: true);
                break;
            }
            catch (DirectoryNotFoundException)
            {
                break;
            }
            catch (IOException ex) when (attempt < 3 && (WinErrorCode)ex.HResult == WinErrorCode.DirNotEmpty)
            {
                context.Information("Another process added files to the directory while its contents were being deleted. Retrying...");
            }
        }
    }
}

private enum WinErrorCode : ushort
{
    DirNotEmpty = 145
}

public sealed class TempDirectory : IDisposable
{
    public DirectoryPath Path { get; }

    public TempDirectory()
    {
        Path = new DirectoryPath(System.IO.Path.GetTempPath()).Combine(System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(Path.FullPath);
    }

    public void Dispose()
    {
        System.IO.Directory.Delete(Path.FullPath, recursive: true);
    }

    public static implicit operator DirectoryPath(TempDirectory tempDirectory)
    {
        return tempDirectory.Path;
    }
}

ProjectFixture NewProjectFixture(DirectoryPath projectDirectory, string configuration, DirectoryPath testResultsDirectory)
{
    return new ProjectFixture(Context, projectDirectory, configuration, testResultsDirectory);
}

private sealed class ProjectFixture
{
    private readonly ICakeContext _context;
    private readonly DirectoryPath _projectDirectory;
    private readonly string _configuration;
    private readonly DirectoryPath _testResultsDirectory;

    private string ProjectName => _projectDirectory.GetDirectoryName();

    public ProjectFixture(ICakeContext context, DirectoryPath projectDirectory, string configuration, DirectoryPath testResultsDirectory)
    {
        _context = context;
        _projectDirectory = projectDirectory;
        _configuration = configuration;
        _testResultsDirectory = testResultsDirectory;
    }

    public void Build(string adapterPackageVersion)
    {
        _context.DeleteDirectoryRobust(_projectDirectory.Combine("bin"));

        var projectFile = _projectDirectory.CombineWithFilePath(ProjectName + ".csproj");
        _context.XmlPoke(projectFile, "/Project/ItemGroup/PackageReference[@Include = 'NUnit3TestAdapter']/@Version", adapterPackageVersion);

        _context.MSBuild(_projectDirectory.FullPath, new MSBuildSettings
        {
            WorkingDirectory = _projectDirectory,
            Verbosity = Verbosity.Minimal,
            Configuration = _configuration,
        }.WithRestore());
    }

    public FilePath Test(string targetFramework)
    {
        var resultsFile = _testResultsDirectory.CombineWithFilePath($"{ProjectName}-{targetFramework}.trx");

        _context.DotNetCoreTest(_projectDirectory.FullPath, new DotNetCoreTestSettings
        {
            Configuration = _configuration,
            Framework = targetFramework,
            NoBuild = true,
            Logger = "trx;LogFileName=" + resultsFile.GetFilename(),
            ResultsDirectory = resultsFile.GetDirectory()
        });

        return resultsFile;
    }
}
