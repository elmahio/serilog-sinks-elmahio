$solution = "$project.sln"
$test = "test\\Serilog.Sinks.ElmahIo.Test\\project.json"
$projectFolder = "src\\Serilog.Sinks.ElmahIo"
$project = $projectFolder + "\\project.json"

function Invoke-Build()
{
    Write-Output "Building"

	if(Test-Path .\artifacts) {
		echo "build: Cleaning .\artifacts"
		Remove-Item .\artifacts -Force -Recurse
	}


    & dotnet restore $test --verbosity Warning
    & dotnet restore $project --verbosity Warning

    Write-Host "Setting version to $env:APPVEYOR_BUILD_VERSION"
    (Get-Content $project).replace("1.0.0-*", $env:APPVEYOR_BUILD_VERSION) | Set-Content $project
	
    & dotnet test $test -c Release
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "The tests failed"
        exit 1 
    }
  
    & dotnet pack $project -c Release -o .\artifacts 
  
    if($LASTEXITCODE -ne 0) 
    {
        Write-Output "Packing the sink failed"
        exit 1 
    }
    Write-Output "Building done"
}

$ErrorActionPreference = "Stop"
Invoke-Build 