#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM node:18.16-slim AS node
WORKDIR /app
COPY ["ui/vue-opentask", "./"]
RUN npm config set registry https://registry.npmmirror.com
RUN npm i
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 1883

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OpenTask.Master/OpenTask.Master.csproj", "OpenTask.Master/"]
COPY ["OpenTask.Core/OpenTask.Core.csproj", "OpenTask.Core/"]
COPY ["OpenTask.Service/OpenTask.Service.csproj", "OpenTask.Service/"]
COPY ["OpenTask.Entity/OpenTask.Entity.csproj", "OpenTask.Entity/"]
RUN dotnet restore "OpenTask.Master/OpenTask.Master.csproj"
COPY . .
WORKDIR "/src/OpenTask.Master"
RUN dotnet build "OpenTask.Master.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "OpenTask.Master.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node /app/dist ./wwwroot
ENTRYPOINT ["dotnet", "OpenTask.Master.dll"]