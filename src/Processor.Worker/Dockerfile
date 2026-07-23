FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# COPY certs/ /usr/local/share/ca-certificates/extra/
# RUN update-ca-certificates

COPY ["src/Processor.Worker/Processor.Worker.csproj", "src/Processor.Worker/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Processor.Worker/Processor.Worker.csproj"

COPY . .

WORKDIR /src/src/Processor.Worker

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Processor.Worker.dll"]