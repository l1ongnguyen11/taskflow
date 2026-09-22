# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/TaskFlow.API/TaskFlow.API.csproj", "backend/TaskFlow.API/"]
COPY ["backend/TaskFlow.Application/TaskFlow.Application.csproj", "backend/TaskFlow.Application/"]
COPY ["backend/TaskFlow.Domain/TaskFlow.Domain.csproj", "backend/TaskFlow.Domain/"]
COPY ["backend/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj", "backend/TaskFlow.Infrastructure/"]

RUN dotnet restore "backend/TaskFlow.API/TaskFlow.API.csproj"

COPY . .
WORKDIR "/src/backend/TaskFlow.API"

RUN dotnet publish "TaskFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]