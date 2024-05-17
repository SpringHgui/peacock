#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM node:18.16-slim AS node
WORKDIR /app
COPY ["ui/vue-scheduler", "./"]
RUN npm config set registry https://registry.npmmirror.com
RUN npm i
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 1883

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Scheduler.Master/Scheduler.Master.csproj", "Scheduler.Master/"]
COPY ["Scheduler.Core/Scheduler.Core.csproj", "Scheduler.Core/"]
COPY ["Scheduler.Service/Scheduler.Service.csproj", "Scheduler.Service/"]
COPY ["Scheduler.Entity/Scheduler.Entity.csproj", "Scheduler.Entity/"]
RUN dotnet restore "Scheduler.Master/Scheduler.Master.csproj"
COPY . .
WORKDIR "/src/Scheduler.Master"
RUN dotnet build "Scheduler.Master.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "Scheduler.Master.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node /app/dist ./wwwroot
ENTRYPOINT ["dotnet", "Scheduler.Master.dll"]