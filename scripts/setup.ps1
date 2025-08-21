$hookSource = "../hooks/pre-commit.ps1"
$hookDestination = "../.git/hooks/pre-commit.ps1"

if (Test-Path $hookSource) {
   Copy-Item $hookSource $hookDestination -Force
   Write-Host "Git pre-commit hooks installed successfully"
} else {
   Write-Error "Source hook files not found: $hookSource"
   exit 1
}