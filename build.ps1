param(
    [string]$p1 = "Debug"
)

dotnet restore
dotnet build ".\src\BetterReadLine" -c $p1
dotnet build ".\src\BetterReadLine.Demo" -c $p1
dotnet build ".\test\BetterReadLine.Tests" -c $p1
