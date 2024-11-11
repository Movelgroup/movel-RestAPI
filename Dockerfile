# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the entire solution
COPY . ./

# Restore & publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configure for Cloud Run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PORT=8080
EXPOSE 8080

# Set the entry point using your actual DLL name
ENTRYPOINT ["dotnet", "rest-api.dll"]