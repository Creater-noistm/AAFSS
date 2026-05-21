# AAFSS GitHub Push Script
# Run this in D:\2026\试点10\AAFSS

Write-Host "=== AAFSS GitHub Push ===" -ForegroundColor Cyan

# Set remote
git remote remove origin 2>$null
git remote add origin https://github.com/Creater-noistm/AAFSS.git

# Switch to main branch
git branch -M main

# Push with credentials prompt
Write-Host "Pushing to Creater-noistm/AAFSS..." -ForegroundColor Yellow
git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n🎉 Code pushed! GitHub Actions will build automatically." -ForegroundColor Green
    Write-Host "Watch: https://github.com/Creater-noistm/AAFSS/actions"
} else {
    Write-Host "`nPush failed. You may need to authenticate." -ForegroundColor Red
    Write-Host "Try: gh auth login"
    Write-Host "Or use a personal access token."
}
