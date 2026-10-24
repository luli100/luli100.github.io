$ErrorActionPreference = 'Stop'
$postPath = Join-Path $PSScriptRoot '../../_posts/2019-04-27-how-to-create-wpf-single-instance-app.md'
$post = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $postPath).Path)
$snippets = [regex]::Matches($post, '(?ms)^```csharp\r?\n(.*?)^```')
$anchors = [regex]::Matches($post, '(?m)^    anchor: (.+)\r?$') |
    ForEach-Object { $_.Groups[1].Value.Trim() }
$headings = [regex]::Matches($post, '(?m)^## .+ \{#([^}]+)\}') |
    ForEach-Object { $_.Groups[1].Value }
if (($anchors -join ',') -ne ($headings -join ',') -or $anchors.Count -ne 9) {
    throw 'Article TOC does not match its nine sections.'
}
if ([regex]::Matches($post, '(?m)^```').Count % 2 -ne 0) { throw 'Unbalanced Markdown fences.' }
if ($snippets.Count -ne 8) { throw 'Article C# examples changed; review the verification mapping.' }

function Normalize-Example([string] $value) {
    $withoutComments = [regex]::Replace($value, '//[^\r\n]*', '')
    return [regex]::Replace($withoutComments, '\s+', '')
}
$source = (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File |
    ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) }) -join "`n"
$normalizedSource = Normalize-Example $source
foreach ($snippet in $snippets) {
    if (-not $normalizedSource.Contains((Normalize-Example $snippet.Groups[1].Value))) {
        throw ('Article snippet differs from compiled source: ' + $snippet.Groups[1].Value.Split("`n")[0])
    }
}
$appXml = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'App.xaml'))
if (-not $post.Replace("`r`n", "`n").Contains($appXml.Replace("`r`n", "`n").Trim())) {
    throw 'App.xaml differs from the article.'
}

dotnet build (Join-Path $PSScriptRoot 'SingleInstanceDemo.csproj') --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'WPF demo build failed.' }
Write-Output 'Verified 8 C# excerpts against compiled sources, App.xaml, 9 TOC anchors and code fences.'
