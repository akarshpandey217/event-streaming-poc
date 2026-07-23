FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# COPY certs/ /usr/local/share/ca-certificates/extra/
# RUN update-ca-certificates

COPY ["src/Ingest.Api/Ingest.Api.csproj", "src/Ingest.Api/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Ingest.Api/Ingest.Api.csproj"

COPY . .

WORKDIR /src/src/Ingest.Api

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ingest.Api.dll"]