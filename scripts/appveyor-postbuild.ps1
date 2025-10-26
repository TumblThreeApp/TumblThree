# Harvest Data
$harvestPath = "$env:APPVEYOR_BUILD_FOLDER\src\TumblThree\TumblThree.Presentation\bin\$env:PLATFORM\Release"
$fileVersion = (Get-Item "$harvestPath\TumblThree.exe").VersionInfo.ProductVersion
[VERSION]$vs = $fileVersion -replace '^.+((\d+\.){3}\d+).+', '$1'
$version = '{0}.{1}.{2}' -f $vs.Major,$vs.Minor,$vs.Build

# Artifacts Paths
$artifactsPath = "$env:APPVEYOR_BUILD_FOLDER\artifacts"
$applicationArtifactsPath = "$artifactsPath\Application\TumblThree"

New-Item -ItemType Directory -Force -Path $applicationArtifactsPath

# Copy in Application Artifacts
Get-ChildItem -Path "$harvestPath\*" -Include *.exe,*.dll,*.config | Copy-Item -Destination $applicationArtifactsPath
#New-Item -ItemType Directory -Force -Path "$applicationArtifactsPath\en"
#Get-ChildItem -Path "$harvestPath\en\*" | Copy-Item -Destination "$applicationArtifactsPath\en"

# Licenses
Copy-Item "$env:APPVEYOR_BUILD_FOLDER\LICENSE" -Destination "$applicationArtifactsPath\LICENSE.txt"
Copy-Item "$env:APPVEYOR_BUILD_FOLDER\LICENSE-3RD-PARTY" -Destination "$applicationArtifactsPath\LICENSE-3RD-PARTY.txt"

# Copy in Translation Artifacts
$translationFolders = dir -Directory $harvestPath | where-object { $_.Name.Length -eq 2 }
foreach ($tf in $translationFolders) {
    $tfTarget = "$applicationArtifactsPath\$tf"
    New-Item -ItemType Directory -Force -Path "$tfTarget"
    Get-ChildItem -Path "$harvestPath\$tf\*" | Copy-Item -Destination "$tfTarget"
}

# Copy in some other Artifacts
$otherFolders = @('x64', 'x86')
foreach ($of in $otherFolders) {
    $ofTarget = "$applicationArtifactsPath\$of"
    New-Item -ItemType Directory -Force -Path "$ofTarget"
    Get-ChildItem -Path "$harvestPath\$of\*" | Copy-Item -Destination "$ofTarget"
}

# Zip Application
$applicationZipPath = "$artifactsPath\TumblThree-v$version-$env:PLATFORM-Application.zip"
Compress-Archive -Path "$applicationArtifactsPath\*" -DestinationPath "$applicationZipPath"
