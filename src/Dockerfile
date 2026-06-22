# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY shared/Transcrib.Shared.csproj shared/
COPY api/Transcrib.Api.csproj api/
RUN dotnet restore api/Transcrib.Api.csproj

COPY shared/ shared/
COPY api/ api/
RUN dotnet publish api/Transcrib.Api.csproj -c Release -o /app /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Transcrib.Api.dll"]
