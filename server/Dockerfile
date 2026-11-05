# Build context: the server/ folder.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached.
COPY API/API.csproj API/
COPY Service/Service.csproj Service/
COPY Infra/Infra.csproj Infra/
RUN dotnet restore API/API.csproj

COPY . .
RUN dotnet publish API/API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/app.db"

# SQLite file lives in a volume; the app runs as the image's non-root user.
RUN mkdir /data && chown $APP_UID /data
VOLUME /data

COPY --from=build /app .
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
