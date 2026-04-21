FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ToolExecutionService.sln ./

COPY ToolExecution.API/ ToolExecution.API/
COPY ToolExecution.Application/ ToolExecution.Application/
COPY ToolExecution.Domain/ ToolExecution.Domain/
COPY ToolExecution.Infrastructure/ ToolExecution.Infrastructure/
COPY ToolExecutionService/ ToolExecutionService/

RUN dotnet restore

COPY . .

WORKDIR /src/ToolExecution.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ToolExecution.API.dll"]