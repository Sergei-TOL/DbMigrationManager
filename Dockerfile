FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DbMigrationsConsole/DbMigrationsConsole.csproj", "DbMigrationsConsole/"]
RUN dotnet restore "DbMigrationsConsole/DbMigrationsConsole.csproj"
COPY . .
WORKDIR "/src/DbMigrationsConsole"
RUN dotnet build "DbMigrationsConsole.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DbMigrationsConsole.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DbMigrationsConsole.dll"] 