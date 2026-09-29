$ErrorActionPreference = "Stop"

Write-Host "Creating solution..."
dotnet new sln -n SwimClub --force

Write-Host "Creating source projects..."
dotnet new classlib -n SwimClub.Domain -o src/SwimClub.Domain -f net8.0 --force
dotnet new classlib -n SwimClub.Application -o src/SwimClub.Application -f net8.0 --force
dotnet new classlib -n SwimClub.Infrastructure -o src/SwimClub.Infrastructure -f net8.0 --force
dotnet new wpf -n SwimClub.UI -o src/SwimClub.UI -f net8.0 --force

Write-Host "Creating test projects..."
dotnet new xunit -n SwimClub.Domain.Tests -o tests/SwimClub.Domain.Tests -f net8.0 --force
dotnet new xunit -n SwimClub.Application.Tests -o tests/SwimClub.Application.Tests -f net8.0 --force
dotnet new xunit -n SwimClub.Infrastructure.Tests -o tests/SwimClub.Infrastructure.Tests -f net8.0 --force
dotnet new xunit -n SwimClub.UI.Tests -o tests/SwimClub.UI.Tests -f net8.0 --force

Write-Host "Adding projects to solution..."
dotnet sln add src/SwimClub.Domain/SwimClub.Domain.csproj src/SwimClub.Application/SwimClub.Application.csproj src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj src/SwimClub.UI/SwimClub.UI.csproj tests/SwimClub.Domain.Tests/SwimClub.Domain.Tests.csproj tests/SwimClub.Application.Tests/SwimClub.Application.Tests.csproj tests/SwimClub.Infrastructure.Tests/SwimClub.Infrastructure.Tests.csproj tests/SwimClub.UI.Tests/SwimClub.UI.Tests.csproj

Write-Host "Configuring references..."
dotnet add src/SwimClub.Application/SwimClub.Application.csproj reference src/SwimClub.Domain/SwimClub.Domain.csproj
dotnet add src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj reference src/SwimClub.Application/SwimClub.Application.csproj
dotnet add src/SwimClub.UI/SwimClub.UI.csproj reference src/SwimClub.Application/SwimClub.Application.csproj
dotnet add src/SwimClub.UI/SwimClub.UI.csproj reference src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj

dotnet add tests/SwimClub.Domain.Tests/SwimClub.Domain.Tests.csproj reference src/SwimClub.Domain/SwimClub.Domain.csproj
dotnet add tests/SwimClub.Application.Tests/SwimClub.Application.Tests.csproj reference src/SwimClub.Application/SwimClub.Application.csproj
dotnet add tests/SwimClub.Infrastructure.Tests/SwimClub.Infrastructure.Tests.csproj reference src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj
dotnet add tests/SwimClub.UI.Tests/SwimClub.UI.Tests.csproj reference src/SwimClub.UI/SwimClub.UI.csproj

Write-Host "Adding NuGet packages..."
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package CommunityToolkit.Mvvm
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package Microsoft.Extensions.DependencyInjection
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package Serilog
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package Serilog.Extensions.Hosting
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package Serilog.Sinks.File
dotnet add src/SwimClub.UI/SwimClub.UI.csproj package MaterialDesignThemes

dotnet add src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj package Serilog
dotnet add src/SwimClub.Infrastructure/SwimClub.Infrastructure.csproj package BCrypt.Net-Next

dotnet add src/SwimClub.Application/SwimClub.Application.csproj package FluentValidation

Write-Host "Setup completed successfully."
