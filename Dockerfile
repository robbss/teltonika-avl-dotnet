FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY properties/ properties/
COPY src/Teltonika.Avl/Teltonika.Avl.csproj src/Teltonika.Avl/
COPY src/Teltonika.Simulator/Teltonika.Simulator.csproj src/Teltonika.Simulator/
RUN dotnet restore src/Teltonika.Simulator/Teltonika.Simulator.csproj
COPY src/Teltonika.Avl/ src/Teltonika.Avl/
COPY src/Teltonika.Simulator/ src/Teltonika.Simulator/
RUN dotnet publish src/Teltonika.Simulator/Teltonika.Simulator.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Teltonika.Simulator.dll"]
