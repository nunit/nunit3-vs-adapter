$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath $PSScriptRoot

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Translate short argument forms to long forms for Cake
$cakeArgs = @()
for ($i = 0; $i -lt $args.Count; $i++) {
    $arg = $args[$i]
    if ($arg -eq '-t' -and $i + 1 -lt $args.Count) {
        $cakeArgs += "--target=$($args[$i + 1])"
        $i++
    }
    elseif ($arg -eq '-c' -and $i + 1 -lt $args.Count) {
        $cakeArgs += "--configuration=$($args[$i + 1])"
        $i++
    }
    elseif ($arg -eq '-v' -and $i + 1 -lt $args.Count) {
        $cakeArgs += "--verbosity=$($args[$i + 1])"
        $i++
    }
    else {
        $cakeArgs += $arg
    }
}

dotnet cake @cakeArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
