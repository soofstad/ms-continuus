FROM mcr.microsoft.com/dotnet/sdk:10.0 AS develop
WORKDIR /app
ADD MSContinuus.csproj ./
RUN dotnet restore
ADD src LICENSE README.md ./
RUN dotnet build
#ENTRYPOINT ["ls", "-la", "/app/bin/Debug/net10.0/"]
#ENTRYPOINT ["dotnet", "watch" ,"run", "--project", "./MSContinuus.csproj"]
ENTRYPOINT ["dotnet", "--info"]

FROM develop as build
RUN dotnet build "./MSContinuus.csproj" -o "/app/build"
RUN dotnet publish "./MSContinuus.csproj" -o "/app/publish"

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS run
LABEL org.opencontainers.image.source="https://github.com/equinor/ms-continuus"
WORKDIR /app

COPY --from=build /app/publish .
ADD src/version /app/version

#RUN groupadd -g 1000 dotnet-non-root-group
#RUN useradd -u 1000 -g dotnet-non-root-group dotnet-non-root-user && chown -R 1000 /app
#USER 1000

ENTRYPOINT ["dotnet", "MSContinuus.dll"]
