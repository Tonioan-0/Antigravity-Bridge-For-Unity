# Test Phase 3 APIs: Physics, Animation, Particles, Screenshot

$baseUrl = "http://localhost:8080"

function Test-Api {
    param([string]$Name, [string]$Method, [string]$Endpoint, [object]$Body = $null)
    
    try {
        $url = "$baseUrl$Endpoint"
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 10
        } else {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $url -Method POST -Body $jsonBody -Headers @{"Content-Type"="application/json"} -TimeoutSec 10
        }
        
        if ($response.status -eq "success") {
            Write-Host "  [PASS] $Name - $($response.message)" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $Name - $($response.message)" -ForegroundColor Red
        }
        return $response
    }
    catch {
        Write-Host "  [ERROR] $Name - $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host "  PHASE 3 API TESTS" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta

# Setup - Create test objects
Write-Host ""
Write-Host "  Setting up test objects..." -ForegroundColor Yellow
Test-Api -Name "Create physics cube" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "PhysicsCube"
    position = @{x=0; y=5; z=0}
    components = @("MeshFilter", "MeshRenderer", "BoxCollider", "Rigidbody")
}

Test-Api -Name "Create floor" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "Floor"
    position = @{x=0; y=0; z=0}
    scale = @{x=10; y=0.1; z=10}
    components = @("MeshFilter", "MeshRenderer", "BoxCollider")
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  1. PHYSICS API TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Get current gravity
Test-Api -Name "Get current gravity" -Method "POST" -Endpoint "/unity/physics/gravity" -Body @{}

# Set custom gravity
Test-Api -Name "Set gravity to -20" -Method "POST" -Endpoint "/unity/physics/gravity" -Body @{
    gravity = @{x=0; y=-20; z=0}
}

# Simulate physics
Test-Api -Name "Simulate physics for 0.5s" -Method "POST" -Endpoint "/unity/physics/simulate" -Body @{
    seconds = 0.5
}

# Step physics
Test-Api -Name "Step physics 10 times" -Method "POST" -Endpoint "/unity/physics/step" -Body @{
    steps = 10
}

# Raycast down
Test-Api -Name "Raycast from (0,10,0) down" -Method "POST" -Endpoint "/unity/physics/raycast" -Body @{
    origin = @{x=0; y=10; z=0}
    direction = @{x=0; y=-1; z=0}
    maxDistance = 100
}

# Reset gravity
Test-Api -Name "Reset gravity to default" -Method "POST" -Endpoint "/unity/physics/gravity" -Body @{
    gravity = @{x=0; y=-9.81; z=0}
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  2. ANIMATION API TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# These will work if objects have Animator/Animation components
Test-Api -Name "Play animation (no animator - expected)" -Method "POST" -Endpoint "/unity/animation/play" -Body @{
    objects = @("PhysicsCube")
    stateName = "Idle"
}

Test-Api -Name "Stop animation" -Method "POST" -Endpoint "/unity/animation/stop" -Body @{
    objects = @("PhysicsCube")
}

Test-Api -Name "Set animator param (no animator - expected)" -Method "POST" -Endpoint "/unity/animator/set" -Body @{
    objects = @("PhysicsCube")
    parameterName = "Speed"
    floatValue = 1.5
    parameterType = "float"
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  3. PARTICLE API TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Create a particle system first
Test-Api -Name "Create particle system" -Method "POST" -Endpoint "/unity/scene/create" -Body @{
    name = "TestParticles"
    position = @{x=0; y=2; z=0}
    components = @("ParticleSystem")
}

# Play particles
Test-Api -Name "Play particles" -Method "POST" -Endpoint "/unity/particles/play" -Body @{
    objects = @("TestParticles")
    withChildren = $true
}

# Emit burst
Test-Api -Name "Emit 50 particles" -Method "POST" -Endpoint "/unity/particles/emit" -Body @{
    objects = @("TestParticles")
    count = 50
}

# Modify particles
Test-Api -Name "Modify particle color to red" -Method "POST" -Endpoint "/unity/particles/modify" -Body @{
    objects = @("TestParticles")
    startSize = 0.5
    startSpeed = 5
    emissionRate = 20
    startColor = @{r=1; g=0; b=0; a=1}
}

# Stop particles
Test-Api -Name "Stop particles" -Method "POST" -Endpoint "/unity/particles/stop" -Body @{
    objects = @("TestParticles")
    clear = $true
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  4. SCREENSHOT API TESTS" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Capture Game View
Test-Api -Name "Capture Game View screenshot" -Method "POST" -Endpoint "/unity/screenshot/capture" -Body @{
    filename = "test_gameview.png"
    superSize = 1
}

# Capture from camera
Test-Api -Name "Capture from Main Camera" -Method "POST" -Endpoint "/unity/screenshot/camera" -Body @{
    cameraName = "Main Camera"
    filename = "test_camera.png"
    width = 1280
    height = 720
}

# Capture Scene View
Test-Api -Name "Capture Scene View" -Method "POST" -Endpoint "/unity/screenshot/scene" -Body @{
    filename = "test_sceneview.png"
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  CLEANUP" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

Test-Api -Name "Delete test objects" -Method "POST" -Endpoint "/unity/scene/delete" -Body @{
    objects = @("PhysicsCube", "Floor", "TestParticles")
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host "  PHASE 3 TESTS COMPLETE" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta
Write-Host ""
