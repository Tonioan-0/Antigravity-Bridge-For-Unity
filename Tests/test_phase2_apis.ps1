# Test Phase 2 APIs: Light, Material, Audio, Script, Tag, Layer
# Run this script with Unity Antigravity Bridge server running

$BaseUrl = "http://localhost:8080"

Write-Host "=== Testing Phase 2 APIs ===" -ForegroundColor Cyan
Write-Host ""

# 1. Test Light API
Write-Host "1. Testing Light API..." -ForegroundColor Yellow
$lightBody = @{
    objects = @("Directional Light")
    color = @{r=1; g=0.5; b=0}
    intensity = 1.5
    shadows = "soft"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/light/modify" -Method POST -Body $lightBody -ContentType "application/json"
    Write-Host "   Light API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Red"})
} catch {
    Write-Host "   Light API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Test Material API
Write-Host "2. Testing Material API..." -ForegroundColor Yellow
$materialBody = @{
    objects = @("Cube")
    color = @{r=0; g=0.5; b=1}
    metallic = 0.7
    smoothness = 0.8
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/material/modify" -Method POST -Body $materialBody -ContentType "application/json"
    Write-Host "   Material API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Red"})
} catch {
    Write-Host "   Material API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Test Tag Create API
Write-Host "3. Testing Tag Create API..." -ForegroundColor Yellow
$tagBody = @{
    name = "TestEnemy"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/tag/create" -Method POST -Body $tagBody -ContentType "application/json"
    Write-Host "   Tag Create API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success" -or $response.message -like "*already exists*"){"Green"}else{"Red"})
} catch {
    Write-Host "   Tag Create API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Test Tag List API
Write-Host "4. Testing Tag List API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/tag/list" -Method GET
    Write-Host "   Tag List API: $($response.status) - Found $($response.data.count) tags" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Red"})
} catch {
    Write-Host "   Tag List API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Test Script Create API
Write-Host "5. Testing Script Create API..." -ForegroundColor Yellow
$scriptBody = @{
    name = "TestPhase2Script"
    path = "Assets/Scripts"
    methods = @("Start", "Update", "OnTriggerEnter")
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/script/create" -Method POST -Body $scriptBody -ContentType "application/json"
    Write-Host "   Script Create API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Red"})
} catch {
    Write-Host "   Script Create API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 6. Test Audio API (modify)
Write-Host "6. Testing Audio Modify API..." -ForegroundColor Yellow
$audioBody = @{
    objects = @("Main Camera")
    volume = 0.8
    pitch = 1.0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/audio/modify" -Method POST -Body $audioBody -ContentType "application/json"
    Write-Host "   Audio Modify API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Yellow"})
} catch {
    Write-Host "   Audio Modify API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

# 7. Test Layer Assign API
Write-Host "7. Testing Layer Assign API..." -ForegroundColor Yellow
$layerBody = @{
    objects = @("Main Camera")
    layer = 0
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/unity/layer/assign" -Method POST -Body $layerBody -ContentType "application/json"
    Write-Host "   Layer Assign API: $($response.status) - $($response.message)" -ForegroundColor $(if($response.status -eq "success"){"Green"}else{"Red"})
} catch {
    Write-Host "   Layer Assign API: Error - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Phase 2 Tests Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary of endpoints tested:" -ForegroundColor White
Write-Host "  POST /unity/light/modify     - Modify light properties"
Write-Host "  POST /unity/material/modify  - Modify material properties"
Write-Host "  POST /unity/tag/create       - Create new tag"
Write-Host "  GET  /unity/tag/list         - List all tags"
Write-Host "  POST /unity/script/create    - Create C# script"
Write-Host "  POST /unity/audio/modify     - Modify AudioSource"
Write-Host "  POST /unity/layer/assign     - Assign layer to objects"
