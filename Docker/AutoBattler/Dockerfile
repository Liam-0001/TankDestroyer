# ==========================================
# STAGE 1: Build
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TankDestroyer.AutoBattler.API/TankDestroyer.AutoBattler.API.csproj \
     TankDestroyer.AutoBattler.API/
COPY Source/TankDestroyer.AutoBattler/TankDestroyer.AutoBattler.csproj \
     Source/TankDestroyer.AutoBattler/
COPY Source/TankDestroyer.Engine/TankDestroyer.Engine.csproj \
     Source/TankDestroyer.Engine/
COPY Source/TankDestroyer.API/TankDestroyer.API.csproj \
     Source/TankDestroyer.API/

RUN dotnet restore "TankDestroyer.AutoBattler.API/TankDestroyer.AutoBattler.API.csproj"

COPY . .

# Publiceer de API
WORKDIR "/src/TankDestroyer.AutoBattler.API"
RUN dotnet publish "TankDestroyer.AutoBattler.API.csproj" \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

# Bouw alle bots en kopieer alleen de *.Bot.dll plat naar /app/publish/Bots
WORKDIR /src
RUN mkdir -p /app/publish/Bots && \
    for proj in Bots/*/*.csproj; do \
      botname=$(basename $(dirname $proj)); \
      dotnet publish "$proj" -c Release -o "/tmp/bots/$botname"; \
      find "/tmp/bots/$botname" -name "*.Bot.dll" -exec cp {} /app/publish/Bots/ \; ; \
    done

# ==========================================
# STAGE 2: Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
COPY Maps ./Maps

ENTRYPOINT ["dotnet", "TankDestroyer.AutoBattler.API.dll"]