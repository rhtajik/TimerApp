FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopier kun .csproj først (for caching)
COPY ["TimerApp.csproj", "./"]
RUN dotnet restore "TimerApp.csproj"

# Kopier resten af koden
COPY . .

# Build
RUN dotnet build "TimerApp.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "TimerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "TimerApp.dll"]