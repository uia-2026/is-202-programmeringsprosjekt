FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Heimevernet.Migrator/Heimevernet.Migrator.csproj

RUN dotnet publish Heimevernet.Migrator/Heimevernet.Migrator.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Heimevernet.Migrator.dll"]