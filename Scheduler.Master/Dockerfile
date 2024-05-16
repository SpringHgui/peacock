#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# USER app
WORKDIR /app
EXPOSE 80
#EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Scheduler.Master/Scheduler.Master.csproj", "Scheduler.Master/"]
COPY ["Scheduler.Core/Scheduler.Core.csproj", "Scheduler.Core/"]
COPY ["Scheduler.Service/Scheduler.Service.csproj", "Scheduler.Service/"]
COPY ["Scheduler.Entity/Scheduler.Entity.csproj", "Scheduler.Entity/"]
RUN dotnet restore "./Scheduler.Master/Scheduler.Master.csproj"
COPY . .
WORKDIR "/src/Scheduler.Master"
RUN dotnet build "./Scheduler.Master.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Scheduler.Master.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Scheduler.Master.dll"]