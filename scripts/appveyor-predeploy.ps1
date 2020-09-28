$apiUrl = 'https://ci.appveyor.com/api'
$token = "$env:DeployApiCode"
$headers = @{
  "Authorization" = "Bearer $token"
  "Content-type" = "application/json"
}
$accountName = 'TumblThreeApp'
$projectSlug = 'Tumblthree'

$downloadLocation = "$env:APPVEYOR_BUILD_FOLDER"
$artifactsPath = "$downloadLocation\artifacts"
New-Item -ItemType Directory -Force -Path $artifactsPath

# get project with last build details
$project = Invoke-RestMethod -Method Get -Uri "$apiUrl/projects/$accountName/$projectSlug" -Headers $headers

foreach($job in $project.build.jobs)
{
  # get this job id
  $jobId = $job.jobId
  if ($jobId -eq "$env:APPVEYOR_JOB_ID") { continue }
  
  # get job artifacts (just to see what we've got)
  $artifacts = Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts" -Headers $headers
  
  foreach($artifact in $artifacts)
  {
    $artifactFileName = $artifact.fileName
    
    # artifact will be downloaded as
    $localArtifactPath = "$downloadLocation\$artifactFileName"
    
    # download artifact
    # -OutFile - is local file name where artifact will be downloaded into
    # the Headers in this call should only contain the bearer token, and no Content-type, otherwise it will fail!
    Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts/$artifactFileName" `
    -OutFile $localArtifactPath -Headers @{ "Authorization" = "Bearer $token" }
  }
}
Get-ChildItem "$artifactsPath\*.zip" | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
