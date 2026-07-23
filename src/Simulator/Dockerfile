FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# COPY certs/ /usr/local/share/ca-certificates/extra/
# RUN update-ca-certificates

COPY ["src/Simulator/Simulator.csproj", "src/Simulator/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Simulator/Simulator.csproj"

COPY . .

WORKDIR /src/src/Simulator

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Simulator.dll"]