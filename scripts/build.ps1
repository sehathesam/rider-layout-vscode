$ErrorActionPreference = 'Stop'

Write-Host 'Restoring .NET solution...'
dotnet restore .\engine\RiderLayout.sln

Write-Host 'Publishing Rider Layout CLI...'
if (Test-Path .\runtime) { Remove-Item .\runtime -Recurse -Force }
dotnet publish .\engine\RiderLayout.Cli\RiderLayout.Cli.csproj -c Release -o .\runtime

Write-Host 'Installing/building VS Code extension...'
npm install
npm run compile

Write-Host 'Packaging VSIX...'
npx @vscode/vsce package

Write-Host 'Done.'
