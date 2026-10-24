$ErrorActionPreference = 'Stop'
$postPath = Join-Path $PSScriptRoot '../../_posts/2023-12-02-csharp-deadlock-in-wpf.md'
$post = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $postPath).Path)
$snippets = [regex]::Matches($post, '(?ms)^```csharp\r?\n(.*?)^```')
if ($snippets.Count -ne 14) {
    throw 'The article code blocks changed. Update the verification wrappers first.'
}

$anchors = [regex]::Matches($post, '(?m)^    anchor: (.+)\r?$') |
    ForEach-Object { $_.Groups[1].Value.Trim() }
$headings = [regex]::Matches($post, '(?m)^## .+ \{#([^}]+)\}') |
    ForEach-Object { $_.Groups[1].Value }
if (($anchors -join ',') -ne ($headings -join ',')) {
    throw 'Table of contents does not match article headings.'
}
if ([regex]::Matches($post, '(?m)^```').Count % 2 -ne 0) {
    throw 'Unbalanced Markdown code fences.'
}

$verifyPath = Join-Path ([System.IO.Path]::GetTempPath()) ('wpf-article-compile-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verifyPath | Out-Null
$generated = New-Object System.Text.StringBuilder
[void]$generated.AppendLine('using System; using System.Diagnostics; using System.Threading; using System.Threading.Tasks; using System.Windows; using System.Windows.Threading;')
for ($index = 0; $index -lt $snippets.Count; $index++) {
    $body = $snippets[$index].Groups[1].Value
    switch ($index) {
        4 { $dependency = $snippets[3].Groups[1].Value; $body += $dependency.Substring($dependency.IndexOf('private async Task<int>')) }
        6 { $body = 'private readonly object _gate = new object(); private int _value;' + $body }
        8 { $body = 'private readonly SemaphoreSlim _serial = new SemaphoreSlim(1, 1);' + $body }
        9 { $body = 'private void CreateCompletion() {' + $body + '}' }
        12 { $body = 'private void Bridge() {' + $body + '}' + $snippets[11].Groups[1].Value }
    }
    [void]$generated.AppendLine("public class ArticleExample$index : Window {")
    [void]$generated.AppendLine($body)
    [void]$generated.AppendLine('}')
}
[System.IO.File]::WriteAllText((Join-Path $verifyPath 'ArticleExamples.cs'), $generated.ToString())
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $verifyPath 'ArticleExamples.csproj') -Encoding utf8
dotnet build (Join-Path $verifyPath 'ArticleExamples.csproj') --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Article examples failed to compile.' }
Write-Output ('Verified 14 C# code blocks, ' + $anchors.Count + ' TOC anchors and balanced code fences.')
Write-Output ('Generated verification project: ' + $verifyPath)
