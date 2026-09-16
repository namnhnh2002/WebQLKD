FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Backend/NamIT.Business.Api/NamIT.Business.Api.csproj
RUN dotnet publish Backend/NamIT.Business.Api/NamIT.Business.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NamIT.Business.Api.dll"]