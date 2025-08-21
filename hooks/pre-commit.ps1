$stagedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.cs$' }
if ($stagedFiles) {
    dotnet format --include ($stagedFiles -join ' ')
    git add $stagedFiles
}
