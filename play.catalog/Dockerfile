FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# ENV ASPNETCORE_URLS=https://+:5001;http://+:5000
# ENV ASPNETCORE_HTTPS_PORTS=5001
# ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

USER root
COPY ./cert/aspnetapp.pfx /etc/ssl/certs/aspnetapp.pfx
COPY ./cert/localhost.crt /etc/ssl/certs/localhost.crt
RUN update-ca-certificates
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY ["src/Play.Catalog.Contracts/Play.Catalog.Contracts.csproj", "src/Play.Catalog.Contracts/"]
COPY ["src/Play.Catalog.Services/Play.Catalog.Service.csproj", "src/Play.Catalog.Services/"]

# RUN --mount=type=secret,id=GH_OWNER,dst=/GH_OWNER --mount=type=secret,id=GH_PAT,dst=/GH_PAT \
#    dotnet nuget add source --username USERNAME --password `cat /GH_PAT` --store-password-in-clear-text --name github "https://nuget.pkg.github.com/`cat /GH_OWNER`/index.json"

RUN dotnet restore "src/Play.Catalog.Services/Play.Catalog.Service.csproj"
COPY ./src ./src
WORKDIR "/src/Play.Catalog.Services"
RUN dotnet publish "Play.Catalog.Service.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Play.Catalog.Service.dll", "https://+:5001;http://+:5000"]
