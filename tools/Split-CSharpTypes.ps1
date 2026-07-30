# Split C# files with multiple top-level types into one-type-per-file.
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Roots
)

function Get-MatchingBraceIndex {
    param([string]$Text, [int]$OpenIndex)
    $depth = 0
    $inString = $false
    $inChar = $false
    $inVerbatim = $false
    $inLineComment = $false
    $inBlockComment = $false
    for ($i = $OpenIndex; $i -lt $Text.Length; $i++) {
        $ch = $Text[$i]
        $nxt = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }

        if ($inLineComment) {
            if ($ch -eq "`n") { $inLineComment = $false }
            continue
        }
        if ($inBlockComment) {
            if ($ch -eq '*' -and $nxt -eq '/') { $inBlockComment = $false; $i++; continue }
            continue
        }
        if ($inString) {
            if ($inVerbatim) {
                if ($ch -eq '"' -and $nxt -eq '"') { $i++; continue }
                if ($ch -eq '"') { $inString = $false; $inVerbatim = $false }
                continue
            }
            if ($ch -eq '\') { $i++; continue }
            if ($ch -eq '"') { $inString = $false }
            continue
        }
        if ($inChar) {
            if ($ch -eq '\') { $i++; continue }
            if ($ch -eq "'") { $inChar = $false }
            continue
        }

        if ($ch -eq '/' -and $nxt -eq '/') { $inLineComment = $true; $i++; continue }
        if ($ch -eq '/' -and $nxt -eq '*') { $inBlockComment = $true; $i++; continue }
        if ($ch -eq '@' -and $nxt -eq '"') { $inString = $true; $inVerbatim = $true; $i++; continue }
        if ($ch -eq '"') { $inString = $true; continue }
        if ($ch -eq "'") { $inChar = $true; continue }

        if ($ch -eq '{') { $depth++ }
        elseif ($ch -eq '}') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }
    throw "Unbalanced braces"
}

function Get-BraceDepthAt {
    param([string]$Text, [int]$From, [int]$To)
    $depth = 0
    $inString = $false
    $inChar = $false
    $inVerbatim = $false
    $inLineComment = $false
    $inBlockComment = $false
    for ($i = $From; $i -lt $To; $i++) {
        $ch = $Text[$i]
        $nxt = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }

        if ($inLineComment) {
            if ($ch -eq "`n") { $inLineComment = $false }
            continue
        }
        if ($inBlockComment) {
            if ($ch -eq '*' -and $nxt -eq '/') { $inBlockComment = $false; $i++; continue }
            continue
        }
        if ($inString) {
            if ($inVerbatim) {
                if ($ch -eq '"' -and $nxt -eq '"') { $i++; continue }
                if ($ch -eq '"') { $inString = $false; $inVerbatim = $false }
                continue
            }
            if ($ch -eq '\') { $i++; continue }
            if ($ch -eq '"') { $inString = $false }
            continue
        }
        if ($inChar) {
            if ($ch -eq '\') { $i++; continue }
            if ($ch -eq "'") { $inChar = $false }
            continue
        }

        if ($ch -eq '/' -and $nxt -eq '/') { $inLineComment = $true; $i++; continue }
        if ($ch -eq '/' -and $nxt -eq '*') { $inBlockComment = $true; $i++; continue }
        if ($ch -eq '@' -and $nxt -eq '"') { $inString = $true; $inVerbatim = $true; $i++; continue }
        if ($ch -eq '"') { $inString = $true; continue }
        if ($ch -eq "'") { $inChar = $true; continue }

        if ($ch -eq '{') { $depth++ }
        elseif ($ch -eq '}') { $depth-- }
    }
    return $depth
}

function Split-CsFile {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path)
    $nsMatch = [regex]::Match($text, '(?m)^(namespace\s+[^\s;{]+[;{])')
    if (-not $nsMatch.Success) { return @() }

    $preamble = $text.Substring(0, $nsMatch.Index)
    $namespaceLine = $nsMatch.Groups[1].Value
    $fileScoped = $namespaceLine.EndsWith(';')
    $bodyStart = $nsMatch.Index + $nsMatch.Length

    $typePattern = '(?m)^(?<indent>\s*)(?<mods>(?:public|internal|protected|private|file|partial|sealed|abstract|static|readonly|ref|required)\s+)+(?<kind>class|record|interface|enum|struct)\s+(?<name>\w+)'
    $all = [regex]::Matches($text.Substring($bodyStart), $typePattern)
    $expectedDepth = if ($fileScoped) { 0 } else { 1 }
    $top = @()
    foreach ($m in $all) {
        $absStart = $bodyStart + $m.Index
        $depth = Get-BraceDepthAt -Text $text -From $bodyStart -To $absStart
        if ($depth -eq $expectedDepth) {
            $top += [PSCustomObject]@{
                Name = $m.Groups['name'].Value
                MatchStart = $absStart
                MatchEnd = $absStart + $m.Length
                MatchText = $m.Value
            }
        }
    }

    if ($top.Count -le 1) { return @() }

    $spans = @()
    foreach ($t in $top) {
        $lineStart = $text.LastIndexOf("`n", $t.MatchStart - 1) + 1
        $cursor = $lineStart
        while ($cursor -gt $bodyStart) {
            $prevNl = $text.LastIndexOf("`n", $cursor - 2)
            $prevLine = if ($prevNl -ge 0) { $text.Substring($prevNl + 1, ($cursor - 1) - ($prevNl + 1)) } else { $text.Substring(0, $cursor - 1) }
            $stripped = $prevLine.Trim()
            if ($stripped -eq '' -or $stripped.StartsWith('//') -or $stripped.StartsWith('///') -or $stripped.StartsWith('[')) {
                $cursor = if ($prevNl -ge 0) { $prevNl + 1 } else { 0 }
                continue
            }
            break
        }

        $searchFrom = $t.MatchEnd
        $brace = $text.IndexOf('{', $searchFrom)
        if ($brace -lt 0) { throw "No body for $($t.Name) in $Path" }
        $semi = $text.IndexOf(';', $searchFrom)
        $isRecordOnly = ($t.MatchText -match '\brecord\b') -and ($t.MatchText -notmatch '\bclass\b') -and ($t.MatchText -notmatch '\bstruct\b')
        if ($isRecordOnly -and $semi -ge 0 -and $semi -lt $brace) {
            $end = $semi
        }
        else {
            $end = Get-MatchingBraceIndex -Text $text -OpenIndex $brace
        }

        $spans += [PSCustomObject]@{
            Name = $t.Name
            Start = $cursor
            End = $end + 1
        }
    }

    $written = @()
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    foreach ($s in $spans) {
        $typeText = $text.Substring($s.Start, $s.End - $s.Start).Trim() + "`n"
        $content = $preamble + $namespaceLine + "`n`n" + $typeText
        $outPath = Join-Path (Split-Path $Path -Parent) ($s.Name + '.cs')
        [System.IO.File]::WriteAllText($outPath, $content, $utf8NoBom)
        $written += $outPath
    }

    $keepNames = @($spans | ForEach-Object { $_.Name + '.cs' })
    $originalName = Split-Path $Path -Leaf
    if ($keepNames -notcontains $originalName) {
        Remove-Item -LiteralPath $Path -Force
    }

    return $written
}

$allWritten = @()
foreach ($root in $Roots) {
    Get-ChildItem -Path $root -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
        ForEach-Object {
            try {
                $written = Split-CsFile -Path $_.FullName
                if ($written.Count -gt 0) {
                    Write-Host ("SPLIT {0} -> {1}" -f $_.FullName, (($written | ForEach-Object { Split-Path $_ -Leaf }) -join ', '))
                    $allWritten += $written
                }
            }
            catch {
                Write-Host ("FAIL {0}: {1}" -f $_.Exception.Message, $_.TargetObject)
            }
        }
}

Write-Host ("Done. Files written: {0}" -f $allWritten.Count)
# Prefer running this script only on multi-type files to avoid duplicate namespace lines.
