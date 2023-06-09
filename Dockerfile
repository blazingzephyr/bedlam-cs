FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Bedlam.csproj", "."]
RUN dotnet restore "./Bedlam.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Bedlam.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bedlam.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bedlam.dll"]