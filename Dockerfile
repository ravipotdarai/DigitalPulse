FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/DigitalPulse.Api/DigitalPulse.Api.csproj -c Release -o /out --no-restore || dotnet publish src/DigitalPulse.Api/DigitalPulse.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DigitalPulse.Api.dll"]
