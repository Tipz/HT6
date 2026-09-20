FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY global.json Directory.Build.props Together.slnx ./
COPY .config .config
COPY src/Together.Core/Together.Core.csproj src/Together.Core/packages.lock.json src/Together.Core/
COPY src/Together.Contracts/Together.Contracts.csproj src/Together.Contracts/packages.lock.json src/Together.Contracts/
COPY src/Together.Client/Together.Client.csproj src/Together.Client/packages.lock.json src/Together.Client/
COPY src/Together.Api/Together.Api.csproj src/Together.Api/packages.lock.json src/Together.Api/
RUN dotnet restore src/Together.Api/Together.Api.csproj --locked-mode \
    && dotnet restore src/Together.Client/Together.Client.csproj --force-evaluate

COPY src src
RUN dotnet publish src/Together.Client/Together.Client.csproj -c Release --no-restore -o /out/client \
    && dotnet publish src/Together.Api/Together.Api.csproj -c Release --no-restore -o /out/api \
    && cp -R /out/client/wwwroot/. /out/api/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS final
WORKDIR /app
COPY --from=build /out/api .
USER root
RUN mkdir -p /app/data-protection && chown -R app:app /app/data-protection
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Together.Api.dll"]
