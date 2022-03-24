
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./Publish.ps1 -ProjectPath "SonicHeroes.Utils.OneRedirector/SonicHeroes.Utils.OneRedirector.csproj" `
              -PackageName "SonicHeroes.Utils.OneRedirector" `

Pop-Location