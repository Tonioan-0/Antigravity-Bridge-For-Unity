# Test Phase 2: Game Element Control APIs
# Run these tests after Unity recompiles

$baseUrl = "http://localhost:8080"

Write-Host "=== Phase 2 API Tests ===" -ForegroundColor Cyan

# Test 1: Light modification
Write-Host "`n1. Test Light Modify" -ForegroundColor Yellow
$lightBody = @{
    objects = @("Directional Light")
    color = @{r=1; g=0.5; b=0; a=1}
    intensity = 2
} | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/unity/light/modify" -Method POST -Body $lightBody -ContentType "application/json"

# Test 2: Get available tags
Write-Host "`n2. Test Tag List" -ForegroundColor Yellow
Invoke-RestMethod -Uri "$baseUrl/unity/tag/list" -Method GET

# Test 3: Create new tag
Write-Host "`n3. Test Tag Create" -ForegroundColor Yellow
$tagBody = @{name = "TestTag"} | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/unity/tag/create" -Method POST -Body $tagBody -ContentType "application/json"

# Test 4: Create script
Write-Host "`n4. Test Script Create" -ForegroundColor Yellow
$scriptBody = @{
    name = "TestController"
    path = "Assets/Scripts"
    methods = @("Start", "Update", "OnTriggerEnter")
} | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/unity/script/create" -Method POST -Body $scriptBody -ContentType "application/json"

Write-Host "`n=== Tests Complete ===" -ForegroundColor Green
