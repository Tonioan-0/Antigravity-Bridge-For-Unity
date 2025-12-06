# Antigravity Unity Bridge v2 - Test Script
# Tests new Editor State API and Output Optimization features

Write-Host "=== Antigravity Unity Bridge v2 - Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Editor State
Write-Host "[1/8] Testing editor state..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/editor/state
Write-Host ""

# Test 2: Console Logs
Write-Host "[2/8] Getting console logs..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/editor/console?limit=10"
Write-Host ""

# Test 3: Console Errors Only
Write-Host "[3/8] Getting console errors only..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/editor/console/errors?limit=5"
Write-Host ""

# Test 4: Compilation Status
Write-Host "[4/8] Getting compilation status..." -ForegroundColor Yellow
curl.exe http://localhost:8080/unity/editor/compilation
Write-Host ""

# Test 5: Hierarchy with depth=1 (only root objects, no deep children)
Write-Host "[5/8] Getting hierarchy with depth=1..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/scene/hierarchy?depth=1"
Write-Host ""

# Test 6: Hierarchy names_only format (minimal output)
Write-Host "[6/8] Getting hierarchy names only (minimal)..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/scene/hierarchy?format=names_only"
Write-Host ""

# Test 7: Object info with select (only components)
Write-Host "[7/8] Getting Main Camera components only..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/scene/objects/Main Camera?select=components"
Write-Host ""

# Test 8: Object info with custom precision
Write-Host "[8/8] Getting object info with 1 decimal precision..." -ForegroundColor Yellow
curl.exe "http://localhost:8080/unity/scene/objects/Main Camera?precision=1"
Write-Host ""

Write-Host "=== v2 API Tests Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "New v2 Features Tested:" -ForegroundColor Cyan
Write-Host "  - /unity/editor/state - Editor state awareness" -ForegroundColor White
Write-Host "  - /unity/editor/console - Console log reading" -ForegroundColor White
Write-Host "  - /unity/editor/compilation - Compilation status" -ForegroundColor White
Write-Host "  - ?depth=N - Hierarchy depth control" -ForegroundColor White
Write-Host "  - ?format=names_only - Minimal output" -ForegroundColor White
Write-Host "  - ?select=components - Field selection" -ForegroundColor White
Write-Host "  - ?precision=N - Transform rounding" -ForegroundColor White
