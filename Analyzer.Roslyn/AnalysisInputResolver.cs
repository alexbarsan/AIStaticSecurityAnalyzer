using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

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

    public static bool ShouldIncludeSourceFile(string filePath) => IsIncludedSourceFile(filePath);

    private static IReadOnlyCollection<string> ResolveSingleFile(string filePath) =>
        IsIncludedSourceFile(filePath) ? [filePath] : Array.Empty<string>();

    private static IReadOnlyCollection<string> ResolveProject(string projectPath)
    {
        var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return ResolveProject(projectPath, visitedProjects);
    }

    private static IReadOnlyCollection<string> ResolveProject(string projectPath, ISet<string> visitedProjects)
    {
        var normalizedProjectPath = Path.GetFullPath(projectPath);
        if (!visitedProjects.Add(normalizedProjectPath))
        {
            return Array.Empty<string>();
        }

        var projectDocument = XDocument.Load(normalizedProjectPath);
        if (ProjectExplicitlyDisablesDefaultCompileItems(projectDocument))
        {
            return ResolveProjectFromXml(normalizedProjectPath, visitedProjects, projectDocument);
        }

        return TryResolveProjectWithMsbuild(normalizedProjectPath, visitedProjects)
            ?? ResolveProjectFromXml(normalizedProjectPath, visitedProjects, projectDocument);
    }

    private static IReadOnlyCollection<string>? TryResolveProjectWithMsbuild(string projectPath, ISet<string> visitedProjects)
    {
        var compileItems = TryGetMsbuildItemFullPaths(projectPath, "Compile");
        if (compileItems == null)
        {
            return null;
        }

        var sourceFiles = compileItems
            .Where(IsIncludedSourceFile)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var projectReferences = TryGetMsbuildItemFullPaths(projectPath, "ProjectReference")
            ?? Array.Empty<string>();

        foreach (var referencedProjectPath in projectReferences
                     .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var referencedSourceFile in ResolveProject(referencedProjectPath, visitedProjects))
            {
                sourceFiles.Add(referencedSourceFile);
            }
        }

        return sourceFiles
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyCollection<string> ResolveProjectFromXml(string projectPath, ISet<string> visitedProjects, XDocument? projectDocument = null)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' does not have a parent directory.");

        projectDocument ??= XDocument.Load(projectPath);
        var defaultCompileItemsEnabled = IsDefaultCompileItemsEnabled(projectDocument);

        var sourceFiles = defaultCompileItemsEnabled
            ? ResolveDirectory(projectDirectory).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var include in GetCompileItemValues(projectDocument, "Include"))
        {
            foreach (var includedPath in ResolveProjectItemPaths(projectDirectory, include))
            {
                if (IsIncludedSourceFile(includedPath))
                {
                    sourceFiles.Add(includedPath);
                }
            }
        }

        foreach (var remove in GetCompileItemValues(projectDocument, "Remove"))
        {
            foreach (var removedPath in ResolveProjectItemPaths(projectDirectory, remove))
            {
                sourceFiles.Remove(removedPath);
            }
        }

        foreach (var projectReference in GetProjectReferenceValues(projectDocument))
        {
            foreach (var referencedProjectPath in ResolveProjectItemPaths(projectDirectory, projectReference))
            {
                if (!referencedProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var referencedSourceFile in ResolveProject(referencedProjectPath, visitedProjects))
                {
                    sourceFiles.Add(referencedSourceFile);
                }
            }
        }

        return sourceFiles
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static bool IsDefaultCompileItemsEnabled(XDocument projectDocument)
    {
        var value = projectDocument
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "EnableDefaultCompileItems")
            ?.Value
            ?.Trim();

        return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProjectExplicitlyDisablesDefaultCompileItems(XDocument projectDocument) =>
        string.Equals(
            projectDocument
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "EnableDefaultCompileItems")
                ?.Value
                ?.Trim(),
            "false",
            StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> GetCompileItemValues(XDocument projectDocument, string attributeName) =>
        projectDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => element.Attribute(attributeName)?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!);

    private static IEnumerable<string> GetProjectReferenceValues(XDocument projectDocument) =>
        projectDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!);

    private static IEnumerable<string> ResolveProjectItemPaths(string projectDirectory, string itemSpec)
    {
        var normalizedItemSpec = itemSpec.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (!ContainsWildcard(normalizedItemSpec))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, normalizedItemSpec));
            if (File.Exists(fullPath))
            {
                yield return fullPath;
            }

            yield break;
        }

        var searchRoot = GetSearchRoot(projectDirectory, normalizedItemSpec);
        if (!Directory.Exists(searchRoot))
        {
            yield break;
        }

        var pattern = Path.GetFileName(normalizedItemSpec);
        var searchOption = normalizedItemSpec.Contains("**", StringComparison.Ordinal)
            ? SearchOption.AllDirectories
            : SearchOption.AllDirectories;

        foreach (var filePath in Directory.EnumerateFiles(searchRoot, pattern, searchOption))
        {
            yield return Path.GetFullPath(filePath);
        }
    }

    private static string GetSearchRoot(string projectDirectory, string itemSpec)
    {
        var firstWildcardIndex = itemSpec.IndexOfAny(['*', '?']);
        if (firstWildcardIndex < 0)
        {
            return projectDirectory;
        }

        var prefix = itemSpec[..firstWildcardIndex];
        var lastSeparatorIndex = prefix.LastIndexOf(Path.DirectorySeparatorChar);
        if (lastSeparatorIndex < 0)
        {
            return projectDirectory;
        }

        var relativeDirectory = prefix[..lastSeparatorIndex];
        return Path.GetFullPath(Path.Combine(projectDirectory, relativeDirectory));
    }

    private static bool ContainsWildcard(string itemSpec) =>
        itemSpec.IndexOfAny(['*', '?']) >= 0;

    private static IReadOnlyCollection<string> GetMsbuildItemFullPaths(string projectPath, string itemName)
    {
        return TryGetMsbuildItemFullPaths(projectPath, itemName)
            ?? throw new InvalidOperationException($"dotnet msbuild did not return '{itemName}' items for '{projectPath}'.");
    }

    private static IReadOnlyCollection<string>? TryGetMsbuildItemFullPaths(string projectPath, string itemName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("msbuild");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add($"-getItem:{itemName}");
            startInfo.ArgumentList.Add("-nologo");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start dotnet msbuild.");

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"dotnet msbuild failed for '{projectPath}' while requesting '{itemName}': {standardError}".Trim());
            }

            using var jsonDocument = JsonDocument.Parse(standardOutput);
            if (!jsonDocument.RootElement.TryGetProperty("Items", out var itemsElement) ||
                !itemsElement.TryGetProperty(itemName, out var itemArray) ||
                itemArray.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return itemArray
                .EnumerateArray()
                .Select(item => item.TryGetProperty("FullPath", out var fullPathElement) ? fullPathElement.GetString() : null)
                .Where(fullPath => !string.IsNullOrWhiteSpace(fullPath))
                .Select(fullPath => Path.GetFullPath(fullPath!))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return null;
        }
    }
}
