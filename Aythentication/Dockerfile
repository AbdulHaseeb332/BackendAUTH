# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Use the .NET 8 SDK image to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Aythentication.csproj", "./"]
RUN dotnet restore "./Aythentication.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Aythentication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aythentication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aythentication.dll"]
