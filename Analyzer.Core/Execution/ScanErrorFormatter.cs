namespace Analyzer.Core.Execution;

public static class ScanErrorFormatter
{
    public static string Format(Exception ex) =>
        ex switch
        {
            FileNotFoundException fileNotFound => $"Analysis path was not found: {fileNotFound.FileName}",
            DirectoryNotFoundException directoryNotFound => $"Analysis directory was not found: {directoryNotFound.Message}",
            NotSupportedException notSupported => notSupported.Message,
            ArgumentException argument => argument.Message,
            _ => $"Analysis failed: {ex.Message}"
        };
}
