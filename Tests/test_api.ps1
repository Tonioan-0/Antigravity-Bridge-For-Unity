# Antigravity Unity Bridge - Test Script
# Run this in PowerShell to test all API endpoints

Write-Host "=== Antigravity Unity Bridge - Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Server Status
Write-Host "[1/7] Testing server status..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/status
Write-Host ""

# Test 2: Scene Info
Write-Host "[2/7] Getting scene info..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/scene/info
Write-Host ""

# Test 3: Scene Hierarchy
Write-Host "[3/7] Getting scene hierarchy..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/scene/hierarchy
Write-Host ""

# Test 4: Available Scripts
Write-Host "[4/7] Getting available scripts..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/project/scripts
Write-Host ""

# Test 5: Create GameObject
Write-Host "[5/7] Creating test GameObject..." -ForegroundColor Yellow
curl.exe -X POST http://localhost:8080/unity/scene/create -H "Content-Type: application/json" -d '{\"name\":\"TestDaAntigravity\",\"position\":{\"x\":0,\"y\":1,\"z\":0}}'
Write-Host ""

# Test 6: Find GameObjects with Light component
Write-Host "[6/7] Finding all Light objects..." -ForegroundColor Yellow
curl.exe -X POST http://localhost:8080/unity/scene/find -H "Content-Type: application/json" -d '{\"filter\":{\"component\":\"Light\"}}'
Write-Host ""

# Test 7: Find and Modify (THE MAIN FEATURE!)
Write-Host "[7/7] Testing Find and Modify (add AudioSource to all Lights)..." -ForegroundColor Yellow
Write-Host "NOTE: This requires a GameObject named 'viale' with Light children" -ForegroundColor Gray
curl.exe -X POST http://localhost:8080/unity/scene/find_and_modify -H "Content-Type: application/json" -d '{\"parent\":\"viale\",\"filter\":{\"component\":\"Light\"},\"operations\":[{\"type\":\"add_component\",\"component\":\"AudioSource\"}]}'
Write-Host ""

Write-Host "=== Tests Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Check Unity Editor:" -ForegroundColor Cyan
Write-Host "  1. Look for 'TestDaAntigravity' object in scene hierarchy" -ForegroundColor White
Write-Host "  2. Check Antigravity Bridge window for command log" -ForegroundColor White
Write-Host "  3. Try Ctrl+Z to undo operations" -ForegroundColor White
