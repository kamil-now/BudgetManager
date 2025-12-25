FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BudgetManager.Api/BudgetManager.Api.csproj", "BudgetManager.Api/"]
RUN dotnet restore "BudgetManager.Api/BudgetManager.Api.csproj"
COPY . .
RUN dotnet build "BudgetManager.Api/BudgetManager.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BudgetManager.Api/BudgetManager.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["BudgetManager.Api/Assets", "Assets/"]
ENTRYPOINT ["dotnet", "BudgetManager.Api.dll"]