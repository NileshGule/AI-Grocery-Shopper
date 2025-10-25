# Multi-stage Dockerfile for the UI (Razor Pages) project
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["UI.csproj", "./"]
RUN dotnet restore "UI.csproj"

# copy everything else and publish
COPY . .
RUN dotnet publish "UI.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS="http://+:80"

COPY --from=build /app/publish ./
EXPOSE 80
ENTRYPOINT ["dotnet", "UI.dll"]
