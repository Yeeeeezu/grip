# grip

fast file content search. readable output, highlighted matches. no config, no deps, one exe.

```
grip <pattern> [path...] [flags]
```

---

## examples

```
> grip "TODO" . --glob *.cs

  src/Main.cs
     42 │ // TODO: handle null case here
    108 │ // TODO: refactor this loop

  src/Utils.cs
    19 │ // TODO: cache this result

3 matches across 2 files
```

```sh
grip -i "error" logs/               # case insensitive
grip -l "password" src/             # filenames only
grip -c "func" --glob *.go          # match count per file
grip "http[s]?://" . --glob *.ts    # regex
```

## flags

| flag | description |
|------|-------------|
| `-i, --ignore-case` | case insensitive |
| `-l, --files` | only print matching filenames |
| `-c, --count` | match count per file |
| `-g, --glob <pattern>` | filter files (e.g. `*.cs`, `*.lua`) |
| `--no-recursive` | don't descend into subdirectories |
| `--no-stats` | suppress the summary line |

## build

```
dotnet build -c Release
dotnet run -- "pattern" path/
```

.NET 8+, all platforms.

## testing

built and ran against a real C# codebase. search, regex, case-insensitive flag, glob filter, stats line all verified. highlights confirmed working.

**not tested:** very large files (>100MB), binary files, symlinks.

## license

MIT
