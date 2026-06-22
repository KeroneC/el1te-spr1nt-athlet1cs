$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$files = @((Get-Item (Join-Path $root "README.md"))) + @(Get-ChildItem (Join-Path $root "docs") -Recurse -Filter *.md)
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $content = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($match in [regex]::Matches($content, '\[[^\]]*\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(https?://|mailto:|#)') { continue }
        $path = ($target -split '#')[0]
        if (-not $path) { continue }
        $resolved = Join-Path $file.DirectoryName ([uri]::UnescapeDataString($path))
        if (-not (Test-Path -LiteralPath $resolved)) {
            $errors.Add("Broken link in $($file.FullName.Substring($root.Length + 1)): $target")
        }
    }

    foreach ($match in [regex]::Matches($content, '`((?:apps|docs|infra|scripts|\.github)/[^`\r\n]+)`')) {
        $candidate = $match.Groups[1].Value.TrimEnd('.')
        if ($candidate -match '[*{}<>]') { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $root $candidate))) {
            $errors.Add("Missing referenced path in $($file.FullName.Substring($root.Length + 1)): $candidate")
        }
    }

    if (([regex]::Matches($content, '(?m)^```')).Count % 2 -ne 0) {
        $errors.Add("Unbalanced Markdown fences in $($file.FullName.Substring($root.Length + 1))")
    }
}

if ($errors.Count) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Documentation validation passed for $($files.Count) Markdown files."
