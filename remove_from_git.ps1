# Remove files from Git tracking without deleting them locally
# This script will remove files from Git's index that shouldn't be tracked
# while keeping the actual files on your disk

Write-Host "Removing files from Git tracking (but keeping them on disk)..."

# Remove Library directory
git rm -r --cached Library/

# Remove Logs directory
git rm -r --cached Logs/

# Remove UserSettings directory
git rm -r --cached UserSettings/

# Remove obj directory
git rm -r --cached obj/

# Remove .vs directory
git rm -r --cached .vs/

# Remove all .csproj files
git rm --cached *.csproj

# Remove all .sln files
git rm --cached *.sln

# Remove Unity Inspector Settings files
git rm --cached "Unity Inspector Settings*"

# Commit the changes
Write-Host "Files have been removed from Git tracking."
Write-Host "To complete the process, commit these changes with:"
Write-Host "git commit -m 'Remove files that should not be tracked'"
Write-Host "git push"
