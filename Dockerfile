#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["WalkSensorAPI.csproj", "."]
RUN dotnet restore "./WalkSensorAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "WalkSensorAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WalkSensorAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN mkdir -p /app/static
COPY ["static/md5.js", "./static/md5.js"]
COPY ["static/test.html", "./static/test.html"]
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalkSensorAPI.dll"]