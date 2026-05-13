FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/InventoryGrpcService/InventoryGrpcService.csproj", "src/InventoryGrpcService/"]
RUN dotnet restore "src/InventoryGrpcService/InventoryGrpcService.csproj"

COPY . .
RUN dotnet publish "src/InventoryGrpcService/InventoryGrpcService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryGrpcService.dll"]
