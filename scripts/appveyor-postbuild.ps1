# Harvest Data
$harvestPath = "$env:APPVEYOR_BUILD_FOLDER\src\TumblThree\TumblThree.Presentation\bin\Release"
$fileVersion = (Get-Item "$harvestPath\TumblThree.exe").VersionInfo.ProductVersion

# Artifacts Paths
$artifactsPath = "$env:APPVEYOR_BUILD_FOLDER\artifacts"
$applicationArtifactsPath = "$artifactsPath\Application\TumbleThree"
$translationArtifactsPath = "$artifactsPath\Translations\TumbleThree"

New-Item -ItemType Directory -Force -Path $applicationArtifactsPath
New-Item -ItemType Directory -Force -Path $translationArtifactsPath

# Copy in Application Artifacts
Get-ChildItem -Path "$harvestPath\*" -Include *.exe,*.dll,*.config | Copy-Item -Destination $applicationArtifactsPath
New-Item -ItemType Directory -Force -Path "$applicationArtifactsPath\en"
Get-ChildItem -Path "$harvestPath\en\*" | Copy-Item -Destination "$applicationArtifactsPath\en"

# Copy in Translation Artifacts
$translationFolders = dir -Directory $harvestPath
foreach ($tf in $translationFolders) {
    $tfTarget = "$translationArtifactsPath\$tf"
    New-Item -ItemType Directory -Force -Path "$tfTarget"
    Get-ChildItem -Path "$harvestPath\$tf\*" | Copy-Item -Destination "$tfTarget"
}

# Zip Application
$applicationZipPath = "$artifactsPath\TumblThree-v$fileVersion-Application.zip"
Compress-Archive -Path "$artifactsPath\Application\TumbleThree\" -DestinationPath "$applicationZipPath"

# Zip Translations
$translationZipPath = "$artifactsPath\TumblThree-v$fileVersion-Translations.zip"
Compress-Archive -Path "$artifactsPath\Translations\TumbleThree\" -DestinationPath "$translationZipPath"
