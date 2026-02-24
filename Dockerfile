FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layered caching)
COPY ToolExecutionService.sln ./
COPY ToolExecution.API/ToolExecution.API.csproj ToolExecution.API/
COPY ToolExecution.Application/ToolExecution.Application.csproj ToolExecution.Application/
COPY ToolExecution.Domain/ToolExecution.Domain.csproj ToolExecution.Domain/
COPY ToolExecution.Infrastructure/ToolExecution.Infrastructure.csproj ToolExecution.Infrastructure/

RUN dotnet restore

# Copy remaining source code
COPY . .

WORKDIR /src/ToolExecution.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ToolExecution.API.dll"]