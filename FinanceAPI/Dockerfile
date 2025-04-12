# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything, then restore and build
COPY . .

# Change this if your .csproj is in a subfolder like /FinanceAPI
RUN dotnet restore "./FinanceAPI/FinanceAPI.csproj"
RUN dotnet publish "./FinanceAPI/FinanceAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# Expose the Render-assigned port
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 10000

ENTRYPOINT ["dotnet", "FinanceAPI.dll"]
