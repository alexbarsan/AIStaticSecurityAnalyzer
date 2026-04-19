namespace Analyzer.Roslyn;

internal static class AnalysisInputResolver
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git",
        ".vs"
    };

    private static readonly string[] GeneratedFileSuffixes =
    [
        ".g.cs",
        ".g.i.cs",
        ".designer.cs",
        ".generated.cs",
        ".assemblyinfo.cs",
        ".assemblyattributes.cs"
    ];

    public static IReadOnlyCollection<string> ResolveSourceFiles(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Analysis path must not be empty.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            return ResolveDirectory(fullPath);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Analysis path was not found.", fullPath);
        }

        return Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".cs" => ResolveSingleFile(fullPath),
            ".csproj" => ResolveProject(fullPath),
            ".sln" => ResolveSolution(fullPath),
            _ => throw new NotSupportedException($"Unsupported analysis input '{fullPath}'. Expected a directory, .cs, .csproj, or .sln path.")
        };
    }

    public static string GetCompilationName(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            return new DirectoryInfo(fullPath).Name;
        }

        return Path.GetFileNameWithoutExtension(fullPath);
    }

    private static IReadOnlyCollection<string> ResolveSingleFile(string filePath) =>
        IsIncludedSourceFile(filePath) ? [filePath] : Array.Empty<string>();

    private static IReadOnlyCollection<string> ResolveProject(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' does not have a parent directory.");

        return ResolveDirectory(projectDirectory);
    }

    private static IReadOnlyCollection<string> ResolveSolution(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException($"Solution path '{solutionPath}' does not have a parent directory.");

        var projectPaths = File.ReadLines(solutionPath)
            .Select(TryParseSolutionProjectPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(relativePath => Path.GetFullPath(Path.Combine(solutionDirectory, relativePath!)))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sourceFiles = projectPaths
            .SelectMany(ResolveProject)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return sourceFiles;
    }

    private static IReadOnlyCollection<string> ResolveDirectory(string directoryPath) =>
        Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
            .Where(IsIncludedSourceFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? TryParseSolutionProjectPath(string line)
    {
        if (!line.StartsWith("Project(", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = line.Split(',');
        if (parts.Length < 2)
        {
            return null;
        }

        var relativePath = parts[1].Trim().Trim('"');
        return relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? relativePath
            : null;
    }

    private static bool IsIncludedSourceFile(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var fileName = Path.GetFileName(normalizedPath);

        if (GeneratedFileSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var directoryPath = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return true;
        }

        var segments = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !segments.Any(segment => ExcludedDirectories.Contains(segment));
    }
}
