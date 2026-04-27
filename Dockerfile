# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-base

# Install Node.js and npm
RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Build the React shell
COPY src/Codex.Shell/package.json src/Codex.Shell/package-lock.json ./src/Codex.Shell/
RUN npm ci --prefix src/Codex.Shell

COPY src/Codex.Shell/ ./src/Codex.Shell/
RUN npm run build --prefix src/Codex.Shell

# Restore and publish the .NET host
COPY Codex.slnx ./
COPY src/Codex.ModuleSDK/ ./src/Codex.ModuleSDK/
COPY src/Codex.Host/ ./src/Codex.Host/

RUN dotnet publish src/Codex.Host/Codex.Host.csproj -c Release -o /out

# Copy shell build output into wwwroot
RUN cp -r src/Codex.Shell/dist/. /out/wwwroot/

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install Node.js, npm, and git (needed for runtime module building and git polling)
RUN apt-get update && apt-get install -y nodejs npm git && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build-base /out ./

# Create required directories
RUN mkdir -p data wwwroot/assets/modules

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV Codex__ModulesPath=/modules
ENV Codex__ModulesOutputPath=/data/modules
ENV Codex__PollerIntervalSeconds=30

ENTRYPOINT ["dotnet", "Codex.Host.dll"]
