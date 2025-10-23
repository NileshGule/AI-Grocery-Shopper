# Build and publish the ShopperAgent
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["src/Agents/ShopperAgent/ShopperAgent.csproj", "src/Agents/ShopperAgent/"]
COPY ["src/Common/Common.csproj", "src/Common/"]

RUN dotnet restore "src/Agents/ShopperAgent/ShopperAgent.csproj"

# copy everything and publish
COPY . .
WORKDIR "/src/src/Agents/ShopperAgent"
RUN dotnet publish "ShopperAgent.csproj" -c Release -o /app/publish /p:UseAppHost=false

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ShopperAgent.dll"]
