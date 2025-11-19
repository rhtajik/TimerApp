FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TimerApp/TimerApp.csproj", "TimerApp/"]
RUN dotnet restore "TimerApp/TimerApp.csproj"
COPY . .
WORKDIR "/src/TimerApp"
RUN dotnet build "TimerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TimerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "TimerApp.dll"]