using System.Text.RegularExpressions;

namespace Grip;

static class Program
{
    static int totalMatches;
    static int totalFiles;

    static void Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return; }

        var opts = ParseArgs(args);
        if (opts == null) return;

        var paths = opts.Paths.Count > 0 ? opts.Paths : ["."];
        foreach (var path in paths)
        {
            if (File.Exists(path))
                SearchFile(path, opts);
            else if (Directory.Exists(path))
                SearchDir(path, opts);
            else
                Err($"path not found: {path}");
        }

        if (opts.Stats)
            Console.Error.WriteLine($"\n{totalMatches} match{(totalMatches == 1 ? "" : "es")} across {totalFiles} file{(totalFiles == 1 ? "" : "s")}");
    }

    static void SearchDir(string dir, Options opts)
    {
        var enumerator = new EnumerationOptions
        {
            RecurseSubdirectories = opts.Recursive,
            IgnoreInaccessible = true,
        };

        var pattern = opts.Glob ?? "*";
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(dir, pattern, enumerator); }
        catch (Exception ex) { Err($"cannot read directory: {ex.Message}"); return; }

        foreach (var f in files)
            SearchFile(f, opts);
    }

    static void SearchFile(string path, Options opts)
    {
        string[] lines;
        try { lines = File.ReadAllLines(path); }
        catch { return; } // binary files, permission errors — skip silently

        var fileMatches = new List<(int lineNum, string line, MatchCollection matches)>();

        for (int i = 0; i < lines.Length; i++)
        {
            MatchCollection mc;
            try { mc = opts.Pattern.Matches(lines[i]); }
            catch { continue; }

            if (mc.Count > 0)
                fileMatches.Add((i + 1, lines[i], mc));
        }

        if (fileMatches.Count == 0) return;

        totalFiles++;
        totalMatches += fileMatches.Count;

        if (opts.FilesOnly)
        {
            Console.WriteLine(Dim(path));
            return;
        }

        // file header
        Console.WriteLine($"\n{Cyan(path)}");

        foreach (var (lineNum, line, mc) in fileMatches)
        {
            if (opts.CountOnly) continue;

            string prefix = Dim($"{lineNum,5} │ ");
            string highlighted = HighlightMatches(line, mc, opts.IgnoreCase);
            Console.WriteLine(prefix + highlighted);
        }

        if (opts.CountOnly)
            Console.WriteLine($"  {fileMatches.Count} match{(fileMatches.Count == 1 ? "" : "es")}");
    }

    static string HighlightMatches(string line, MatchCollection mc, bool ignoreCase)
    {
        // rebuild line with match spans highlighted yellow
        var sb = new System.Text.StringBuilder();
        int pos = 0;
        foreach (Match m in mc)
        {
            if (m.Index > pos) sb.Append(line[pos..m.Index]);
            sb.Append($"\x1b[1;33m{line[m.Index..(m.Index + m.Length)]}\x1b[0m");
            pos = m.Index + m.Length;
        }
        if (pos < line.Length) sb.Append(line[pos..]);
        return sb.ToString();
    }

    record Options(
        Regex Pattern,
        List<string> Paths,
        string? Glob,
        bool Recursive,
        bool IgnoreCase,
        bool FilesOnly,
        bool CountOnly,
        bool Stats
    );

    static Options? ParseArgs(string[] args)
    {
        string? rawPattern = null;
        var paths = new List<string>();
        string? glob = null;
        bool recursive = true;
        bool ignoreCase = false;
        bool filesOnly = false;
        bool countOnly = false;
        bool stats = true;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i" or "--ignore-case": ignoreCase = true; break;
                case "-l" or "--files": filesOnly = true; break;
                case "-c" or "--count": countOnly = true; break;
                case "--no-stats": stats = false; break;
                case "-r" or "--recursive": recursive = true; break;
                case "--no-recursive": recursive = false; break;
                case "-g" or "--glob":
                    if (++i >= args.Length) { Err("--glob requires a value"); return null; }
                    glob = args[i];
                    break;
                default:
                    if (args[i].StartsWith('-')) { Err($"unknown flag: {args[i]}"); return null; }
                    if (rawPattern == null) rawPattern = args[i];
                    else paths.Add(args[i]);
                    break;
            }
        }

        if (rawPattern == null) { Err("pattern required"); PrintHelp(); return null; }

        Regex pattern;
        try
        {
            var flags = RegexOptions.Compiled | (ignoreCase ? RegexOptions.IgnoreCase : 0);
            pattern = new Regex(rawPattern, flags);
        }
        catch (RegexParseException ex) { Err($"invalid pattern: {ex.Message}"); return null; }

        return new Options(pattern, paths, glob, recursive, ignoreCase, filesOnly, countOnly, stats);
    }

    static string Cyan(string s) => $"\x1b[36m{s}\x1b[0m";
    static string Dim(string s) => $"\x1b[2m{s}\x1b[0m";
    static void Err(string msg) => Console.Error.WriteLine($"\x1b[31merror:\x1b[0m {msg}");

    static void PrintHelp() => Console.WriteLine("""

  grip — fast file content search

  usage:
    grip <pattern> [path...] [flags]

  flags:
    -i, --ignore-case       case insensitive
    -l, --files             only print matching filenames
    -c, --count             only print match count per file
    -r, --recursive         recurse into directories (default: on)
        --no-recursive      don't recurse
    -g, --glob <pattern>    filter files by glob (e.g. *.cs)
        --no-stats          suppress match count summary

  examples:
    grip "TODO" . --glob *.cs
    grip -i "error" logs/
    grip -l "password" src/
    grip "func\s+\w+" --glob *.go

""");
}
