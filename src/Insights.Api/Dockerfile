FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# COPY certs/ /usr/local/share/ca-certificates/extra/
# RUN update-ca-certificates

COPY ["src/Insights.Api/Insights.Api.csproj", "src/Insights.Api/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Insights.Api/Insights.Api.csproj"

COPY . .

WORKDIR /src/src/Insights.Api

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Insights.Api.dll"]