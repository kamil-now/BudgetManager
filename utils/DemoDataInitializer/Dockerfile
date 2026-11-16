# Build context: the repository root (see docker/docker-compose.dev.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY ["global.json", "Directory.Packages.props", "./"]
COPY ["src/BudgetManager.Common/BudgetManager.Common.csproj", "src/BudgetManager.Common/"]
COPY ["src/BudgetManager.Domain/BudgetManager.Domain.csproj", "src/BudgetManager.Domain/"]
COPY ["src/BudgetManager.Application/BudgetManager.Application.csproj", "src/BudgetManager.Application/"]
COPY ["src/BudgetManager.Infrastructure/BudgetManager.Infrastructure.csproj", "src/BudgetManager.Infrastructure/"]
COPY ["utils/DemoDataInitializer/BudgetManager.DemoDataInitializer.csproj", "utils/DemoDataInitializer/"]
RUN dotnet restore "utils/DemoDataInitializer/BudgetManager.DemoDataInitializer.csproj"
COPY src/BudgetManager.Common/ src/BudgetManager.Common/
COPY src/BudgetManager.Domain/ src/BudgetManager.Domain/
COPY src/BudgetManager.Application/ src/BudgetManager.Application/
COPY src/BudgetManager.Infrastructure/ src/BudgetManager.Infrastructure/
COPY utils/DemoDataInitializer/ utils/DemoDataInitializer/
RUN dotnet publish "utils/DemoDataInitializer/BudgetManager.DemoDataInitializer.csproj" -c Release -o /app --no-restore

# aspnet runtime: Infrastructure references the ASP.NET Core shared framework (JwtBearer)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BudgetManager.DemoDataInitializer.dll"]
