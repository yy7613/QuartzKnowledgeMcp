# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj src/QuartzKnowledgeMcp.Api/
RUN dotnet restore src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj

COPY src ./src
RUN dotnet publish src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Container

EXPOSE 8080
VOLUME ["/data"]

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "QuartzKnowledgeMcp.Api.dll"]