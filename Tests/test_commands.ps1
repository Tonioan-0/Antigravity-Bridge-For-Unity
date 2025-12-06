# Antigravity Unity Bridge v2 - Unix-like Commands Test
# Tests the unified /unity/command endpoint

Write-Host "=== Antigravity Unity Bridge - Unix Commands Test ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Help command
Write-Host "[1/7] Getting help..." -ForegroundColor Yellow
$body = '{"cmd": "help"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 2: Find all objects
Write-Host "[2/7] find . (all objects)..." -ForegroundColor Yellow
$body = '{"cmd": "find . --limit 5 --format names_only"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 3: Find by component
Write-Host "[3/7] find . --component Light..." -ForegroundColor Yellow
$body = '{"cmd": "find . --component Light --format names_only"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 4: Create object
Write-Host "[4/7] create TestCube --position 0,2,0..." -ForegroundColor Yellow
$body = '{"cmd": "create TestCubeFromCommand --position 0,2,0 --components BoxCollider"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 5: Get object info
Write-Host "[5/7] get TestCubeFromCommand..." -ForegroundColor Yellow
$body = '{"cmd": "get TestCubeFromCommand --select components"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 6: Modify object
Write-Host "[6/7] modify TestCubeFromCommand --add Rigidbody..." -ForegroundColor Yellow
$body = '{"cmd": "modify TestCubeFromCommand --add Rigidbody"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

# Test 7: Delete object
Write-Host "[7/7] delete TestCubeFromCommand..." -ForegroundColor Yellow
$body = '{"cmd": "delete TestCubeFromCommand"}'
curl.exe -X POST http://localhost:8080/unity/command -H "Content-Type: application/json" -d $body
Write-Host ""

Write-Host "=== Unix Commands Test Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Available Commands:" -ForegroundColor Cyan
Write-Host "  find . --component Light --format names_only" -ForegroundColor White
Write-Host "  create MyCube --position 0,1,0 --components BoxCollider" -ForegroundColor White
Write-Host "  modify Player --add AudioSource --set active=false" -ForegroundColor White
Write-Host "  delete TempObject --force" -ForegroundColor White
Write-Host "  get MainCamera --select components" -ForegroundColor White
Write-Host "  help" -ForegroundColor White
