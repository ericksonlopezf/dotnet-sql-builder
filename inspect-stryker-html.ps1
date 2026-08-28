param([string]$htmlPath)

$html = Get-Content $htmlPath -Raw
$startToken = "app.report = "
$idx = $html.IndexOf($startToken)
if ($idx -lt 0) {
    Write-Host "app.report not found"
    exit 1
}

$jsonStart = $idx + $startToken.Length
$endIdx = $html.IndexOf(";\n", $jsonStart)
if ($endIdx -lt 0) {
    $endIdx = $html.IndexOf(";\r\n", $jsonStart)
}
if ($endIdx -lt 0) {
    $endIdx = $html.LastIndexOf("};") + 1
}

$jsonStr = $html.Substring($jsonStart, $endIdx - $jsonStart).Trim().TrimEnd(';')
$report = $jsonStr | ConvertFrom-Json

$results = @()
$totalMutants = 0
$totalKilled = 0
$totalSurvived = 0

foreach ($prop in $report.files.PSObject.Properties) {
    $file = $prop.Name
    $mutants = $prop.Value.mutants
    $count = $mutants.Count
    $killed = ($mutants | Where-Object { $_.status -eq 'Killed' }).Count
    $survived = ($mutants | Where-Object { $_.status -eq 'Survived' }).Count
    $timeout = ($mutants | Where-Object { $_.status -eq 'Timeout' }).Count
    $compileError = ($mutants | Where-Object { $_.status -eq 'CompileError' }).Count
    $noCoverage = ($mutants | Where-Object { $_.status -eq 'NoCoverage' }).Count
    
    $totalMutants += $count
    $totalKilled += $killed
    $totalSurvived += ($survived + $noCoverage)
    
    $score = if ($count -gt 0) { [math]::Round(100.0 * $killed / ($count - $compileError), 2) } else { 100 }
    
    $shortName = [System.IO.Path]::GetFileName($file)
    $results += [PSCustomObject]@{
        File = $shortName
        Mutants = $count
        Killed = $killed
        Survived = $survived
        NoCoverage = $noCoverage
        Score = "$score%"
    }
}

$results | Format-Table -AutoSize

Write-Host "TOTAL: $totalKilled / $totalMutants killed. Survived: $totalSurvived"

foreach ($prop in $report.files.PSObject.Properties) {
    $file = $prop.Name
    $shortName = [System.IO.Path]::GetFileName($file)
    $survivors = $prop.Value.mutants | Where-Object { $_.status -eq 'Survived' -or $_.status -eq 'NoCoverage' }
    if ($survivors.Count -gt 0) {
        Write-Host "=== SURVIVORS IN $shortName ===" -ForegroundColor Yellow
        foreach ($s in $survivors) {
            Write-Host "[$($s.id)] Line $($s.location.start.line): $($s.mutatorName) -> $($s.replacement)"
        }
    }
}
