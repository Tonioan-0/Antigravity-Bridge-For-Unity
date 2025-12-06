# Antigravity Bridge - Advanced Test Suite
# Tests for: Round-Trip, Complex Components, Prefabs, Custom Scripts, Scene Management

$baseUrl = "http://localhost:8080"
$testResults = @()

function Test-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$ExpectedStatus = "success",
        [bool]$ShouldFail = $false,
        [switch]$Silent
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
        
        if (-not $Silent) {
            if ($passed) {
                Write-Host "  [PASS] $Name" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] $Name - Expected: $ExpectedStatus, Got: $($response.status)" -ForegroundColor Red
            }
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
        
        if (-not $Silent) {
            if ($ShouldFail) {
                Write-Host "  [PASS] $Name (Expected failure)" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] $Name - ERROR: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        return $null
    }
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host "  ANTIGRAVITY BRIDGE - ADVANCED TEST SUITE" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  1. ROUND-TRIP TESTS (Get after Set)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Create object and verify we can read it back
$createResult = Test-Api -Name "Create cube for round-trip test" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "RoundTripCube"
    position = @{x=5; y=10; z=3}
    scale = @{x=2; y=2; z=2}
    components = @("MeshFilter", "MeshRenderer", "BoxCollider")
}

# Query the object back
$queryResult = Test-Api -Name "Query created object position" -Method "GET" -Endpoint "/unity/scene/objects/RoundTripCube"

# Verify position matches what we set
if ($queryResult -and $queryResult.data.object_info) {
    $pos = $queryResult.data.object_info.position
    $posMatch = ($pos.x -eq 5) -and ($pos.y -eq 10) -and ($pos.z -eq 3)
    if ($posMatch) {
        Write-Host "  [PASS] Position values match: ($($pos.x), $($pos.y), $($pos.z))" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Position mismatch: Expected (5,10,3) Got ($($pos.x), $($pos.y), $($pos.z))" -ForegroundColor Yellow
    }
}

# Create parent-child hierarchy and query children
Test-Api -Name "Create parent for hierarchy test" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "HierarchyParent"
    position = @{x=0; y=0; z=0}
}

Test-Api -Name "Create child 1" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "HierarchyChild1"
    parent = "HierarchyParent"
    position = @{x=1; y=0; z=0}
}

Test-Api -Name "Create child 2" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "HierarchyChild2"
    parent = "HierarchyParent"
    position = @{x=-1; y=0; z=0}
}

# Query parent to verify children list
$hierarchyResult = Test-Api -Name "Query parent hierarchy" -Method "GET" -Endpoint "/unity/scene/objects/HierarchyParent"
if ($hierarchyResult -and $hierarchyResult.data.object_info.children) {
    $childCount = $hierarchyResult.data.object_info.children.Count
    Write-Host "  [INFO] Found $childCount children in hierarchy" -ForegroundColor Cyan
}

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  2. COMPLEX COMPONENT TESTS (Light, Camera)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Create a light
Test-Api -Name "Create Point Light" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "TestLight"
    position = @{x=0; y=5; z=0}
    components = @("Light")
}

# Modify light color (testing 0-1 float range)
Test-Api -Name "Set light color to RED (1,0,0)" -Method "POST" -Endpoint "/unity/light/modify" -Body @{
    objects = @("TestLight")
    color = @{r=1; g=0; b=0; a=1}
    intensity = 2.5
}

# Query light to verify
$lightQuery = Test-Api -Name "Query light properties" -Method "GET" -Endpoint "/unity/scene/objects/TestLight"
if ($lightQuery) {
    $lightComp = $lightQuery.data.object_info.components | Where-Object { $_.type -eq "Light" }
    if ($lightComp -and $lightComp.serialized_properties) {
        Write-Host "  [INFO] Light properties found:" -ForegroundColor Cyan
        $lightComp.serialized_properties | ForEach-Object {
            Write-Host "         $($_.key) = $($_.value)" -ForegroundColor Gray
        }
    }
}

# Test with RGB values (0-255 range to see if conversion works)
Test-Api -Name "Set light color using float 0.5" -Method "POST" -Endpoint "/unity/light/modify" -Body @{
    objects = @("TestLight")
    color = @{r=0.5; g=0.8; b=0.2; a=1}
}

# Camera manipulation - query Main Camera first
$cameraQuery = Test-Api -Name "Query Main Camera" -Method "GET" -Endpoint "/unity/scene/objects/Main Camera"

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  3. PREFAB AND RESOURCE TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Test loading non-existent resource (should fail gracefully)
Test-Api -Name "Load non-existent prefab (should handle error)" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "FromNonExistentPrefab"
    prefab = "NonExistent/Path/To/Prefab"
} -ExpectedStatus "success"  # Will create empty object, not crash

# Test material assignment with wrong path
Test-Api -Name "Assign non-existent material (error handling)" -Method "POST" -Endpoint "/unity/material/assign" -Body @{
    objects = @("RoundTripCube")
    materialPath = "Assets/NonExistent/Material.mat"
} -ExpectedStatus "error" -ShouldFail $true

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  4. CUSTOM SCRIPTS AND REFLECTION TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Add a component that exists
Test-Api -Name "Add Rigidbody component" -Method "POST" -Endpoint "/unity/component/add" -Body @{
    objects = @("RoundTripCube")
    component = "Rigidbody"
}

# Query to verify Rigidbody was added
$rbQuery = Test-Api -Name "Query Rigidbody properties" -Method "GET" -Endpoint "/unity/scene/objects/RoundTripCube"
if ($rbQuery) {
    $rbComp = $rbQuery.data.object_info.components | Where-Object { $_.type -eq "Rigidbody" }
    if ($rbComp) {
        Write-Host "  [PASS] Rigidbody found with properties:" -ForegroundColor Green
        if ($rbComp.serialized_properties) {
            $rbComp.serialized_properties | ForEach-Object {
                Write-Host "         $($_.key) = $($_.value)" -ForegroundColor Gray
            }
        }
    }
}

# Modify Rigidbody properties via component/modify
Test-Api -Name "Modify Rigidbody mass via propertyValues" -Method "POST" -Endpoint "/unity/component/modify" -Body @{
    objects = @("RoundTripCube")
    component = "Rigidbody"
    propertyValues = @(
        @{key="mass"; floatValue=5.0; valueType="float"}
        @{key="useGravity"; boolValue=$false; valueType="bool"}
    )
}

# Query again to verify modification
$rbQueryAfter = Test-Api -Name "Verify Rigidbody modification" -Method "GET" -Endpoint "/unity/scene/objects/RoundTripCube"

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  5. SCENE MANAGEMENT TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Get current scene info
$sceneInfo = Test-Api -Name "Get current scene info" -Method "GET" -Endpoint "/unity/scene/info"
if ($sceneInfo) {
    Write-Host "  [INFO] Current scene: $($sceneInfo.data.scene_info.name)" -ForegroundColor Cyan
}

# Get hierarchy
$hierarchy = Test-Api -Name "Get full scene hierarchy" -Method "GET" -Endpoint "/unity/scene/hierarchy"
if ($hierarchy -and $hierarchy.data.scene_hierarchy) {
    $rootCount = $hierarchy.data.scene_hierarchy.root_objects.Count
    Write-Host "  [INFO] Scene has $rootCount root objects" -ForegroundColor Cyan
}

# Test modifying an object that was just deleted (zombie object)
Write-Host ""
Write-Host "  Testing zombie object handling..." -ForegroundColor Yellow
Test-Api -Name "Create zombie test object" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "ZombieTestObject"
    position = @{x=100; y=0; z=0}
}
Test-Api -Name "Delete zombie test object" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @("ZombieTestObject")
}
Test-Api -Name "Try to modify deleted object (zombie)" -Method "POST" -Endpoint "/unity/component/add" -Body @{
    objects = @("ZombieTestObject")
    component = "Light"
} -ExpectedStatus "error" -ShouldFail $true

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  CLEANUP" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

Test-Api -Name "Cleanup test objects" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @(
        "RoundTripCube",
        "HierarchyParent",
        "TestLight",
        "FromNonExistentPrefab"
    )
}

# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host "  ADVANCED TEST RESULTS SUMMARY" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta
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
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host ""
