# Antigravity Bridge - Comprehensive Test Suite
# Tests for: Logging, CRUD, Transform, Parenting, Type Casting

$baseUrl = "http://localhost:8080"
$testResults = @()

function Test-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$ExpectedStatus = "success",
        [bool]$ShouldFail = $false
    )
    
    try {
        $url = "$baseUrl$Endpoint"
        $headers = @{"Content-Type" = "application/json"}
        
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 10
        } else {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $url -Method POST -Body $jsonBody -Headers $headers -TimeoutSec 10
        }
        
        $passed = ($response.status -eq $ExpectedStatus) -or ($ShouldFail -and $response.status -eq "error")
        
        $script:testResults += @{
            Name = $Name
            Passed = $passed
            Status = $response.status
            Message = $response.message
        }
        
        if ($passed) {
            Write-Host "  [PASS] $Name" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $Name - Expected: $ExpectedStatus, Got: $($response.status)" -ForegroundColor Red
        }
        
        return $response
    }
    catch {
        $script:testResults += @{
            Name = $Name
            Passed = $ShouldFail
            Status = "error"
            Message = $_.Exception.Message
        }
        
        if ($ShouldFail) {
            Write-Host "  [PASS] $Name (Expected failure)" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $Name - ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }
        return $null
    }
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  1. HELLO WORLD and LOGGING TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Simple health check
Test-Api -Name "Server Health Check" -Method "GET" -Endpoint "/unity/health"

# Simple object creation
Test-Api -Name "Simple log via object creation" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "TestLog_HelloWorld"
    position = @{x=0; y=0; z=0}
}

# Special characters test - quotes
Test-Api -Name "Special chars: underscores" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "Test_With_Underscores"
    position = @{x=0; y=1; z=0}
}

# Semicolon test
Test-Api -Name "Special chars: semicolon in name" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "Test_Semi_Colon"
    position = @{x=0; y=2; z=0}
}

# Empty name test
Test-Api -Name "Empty name handling" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = ""
    position = @{x=0; y=3; z=0}
}

# Flood test - 10 rapid requests
Write-Host ""
Write-Host "  Flood Test: Sending 10 rapid requests..." -ForegroundColor Yellow
$floodStart = Get-Date
$floodErrors = 0
for ($i = 1; $i -le 10; $i++) {
    try {
        $body = @{name = "FloodTest_$i"; position = @{x=$i; y=0; z=0}} | ConvertTo-Json
        $response = Invoke-RestMethod -Uri "$baseUrl/unity/scene/create" -Method POST -Body $body -Headers @{"Content-Type"="application/json"} -TimeoutSec 5
        if ($response.status -ne "success") { $floodErrors++ }
    } catch { $floodErrors++ }
}
$floodTime = (Get-Date) - $floodStart
$floodColor = if($floodErrors -eq 0){"Green"}else{"Yellow"}
Write-Host "  [INFO] Flood test: $((10-$floodErrors))/10 succeeded in $($floodTime.TotalSeconds.ToString('F2'))s" -ForegroundColor $floodColor

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  2. GAMEOBJECT CRUD TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Basic creation at origin
Test-Api -Name "Create cube at origin (0,0,0)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "TestCube_Origin"
    position = @{x=0; y=0; z=0}
    components = @("MeshFilter", "MeshRenderer", "BoxCollider")
}

# Long name (50 chars for safety)
$longName = "LongName_" + ("A" * 40)
Test-Api -Name "Create object with long name (50 chars)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = $longName
    position = @{x=5; y=0; z=0}
}

# Numbered name
Test-Api -Name "Create object with numbers: Oggetto_123" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "Oggetto_123"
    position = @{x=10; y=0; z=0}
}

# Create and immediately delete
Write-Host ""
Write-Host "  Race condition test: Create then immediate delete..." -ForegroundColor Yellow
Test-Api -Name "Create for immediate deletion" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "RaceConditionTest"
    position = @{x=15; y=0; z=0}
}
Start-Sleep -Milliseconds 100
Test-Api -Name "Immediate deletion after create" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @("RaceConditionTest")
}

# Delete non-existent object
Test-Api -Name "Delete non-existent Fantasma" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @("Fantasma")
} -ExpectedStatus "success"

# Delete empty array
Test-Api -Name "Delete with empty array" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @()
} -ExpectedStatus "error"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  3. TRANSFORM TESTS (Position, Rotation, Scale)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Negative coordinates
Test-Api -Name "Negative coordinates (-10.5, -5.0, -1)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "NegativeCoordObject"
    position = @{x=-10.5; y=-5.0; z=-1}
}

# Precision test with PI
Test-Api -Name "Decimal precision: 3.14159" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "PrecisionTestObject"
    position = @{x=3.14159; y=2.71828; z=1.41421}
}

# Query to verify precision
Test-Api -Name "Query precision values" -Method "GET" -Endpoint "/unity/scene/objects/PrecisionTestObject"

# Scale to zero
Test-Api -Name "Scale to zero (0,0,0)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "ZeroScaleObject"
    position = @{x=20; y=0; z=0}
    scale = @{x=0; y=0; z=0}
}

# Extreme coordinates
Test-Api -Name "Extreme coordinates (999999)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "ExtremeCoordObject"
    position = @{x=999999; y=999999; z=999999}
}

# Very small numbers
Test-Api -Name "Very small numbers (0.00001)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "SmallNumberObject"
    position = @{x=0.00001; y=0.00001; z=0.00001}
    scale = @{x=0.00001; y=0.00001; z=0.00001}
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  4. PARENTING/HIERARCHY TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Create parent
Test-Api -Name "Create Parent object A" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "ParentA"
    position = @{x=0; y=5; z=0}
}

# Create child with parent
Test-Api -Name "Create Child B under ParentA" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "ChildB"
    parent = "ParentA"
    position = @{x=2; y=0; z=0}
}

# Query hierarchy
Test-Api -Name "Query hierarchy of ParentA" -Method "GET" -Endpoint "/unity/scene/objects/ParentA"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  5. TYPE CASTING and EDGE CASES" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Query non-existent object
Test-Api -Name "Query non-existent object" -Method "GET" -Endpoint "/unity/scene/objects/NonExistentObject123" -ExpectedStatus "error" -ShouldFail $true

# Add component to non-existent object
Test-Api -Name "Add component to non-existent object" -Method "POST" -Endpoint "/unity/component/add" -Body @{
    objects = @("GhostObject")
    component = "Rigidbody"
} -ExpectedStatus "error" -ShouldFail $true

# Add non-existent component
Test-Api -Name "Add non-existent component type" -Method "POST" -Endpoint "/unity/component/add" -Body @{
    objects = @("TestCube_Origin")
    component = "FakeComponentThatDoesNotExist"
} -ExpectedStatus "error" -ShouldFail $true

# Material color test
Test-Api -Name "Material color modification" -Method "POST" -Endpoint "/unity/material/modify" -Body @{
    objects = @("TestCube_Origin")
    color = @{r=0.5; g=0.2; b=0.8; a=1}
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  CLEANUP: Deleting test objects" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

$testObjects = @(
    "TestLog_HelloWorld", "Test_With_Underscores", "Test_Semi_Colon",
    "TestCube_Origin", $longName, "Oggetto_123",
    "NegativeCoordObject", "PrecisionTestObject",
    "ZeroScaleObject", "ExtremeCoordObject", "SmallNumberObject",
    "ParentA"
)

# Add flood test objects
for ($i = 1; $i -le 10; $i++) {
    $testObjects += "FloodTest_$i"
}

Test-Api -Name "Cleanup: Delete all test objects" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = $testObjects
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

$passed = ($testResults | Where-Object { $_.Passed }).Count
$failed = ($testResults | Where-Object { -not $_.Passed }).Count
$total = $testResults.Count

Write-Host "  Total Tests: $total" -ForegroundColor White
Write-Host "  Passed: $passed" -ForegroundColor Green
$failColor = if($failed -gt 0){"Red"}else{"Green"}
Write-Host "  Failed: $failed" -ForegroundColor $failColor
Write-Host ""

if ($failed -gt 0) {
    Write-Host "  Failed Tests:" -ForegroundColor Red
    $testResults | Where-Object { -not $_.Passed } | ForEach-Object {
        Write-Host "    - $($_.Name): $($_.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""
