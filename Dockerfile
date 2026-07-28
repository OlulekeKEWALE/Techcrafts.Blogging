# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy everything into the container
COPY . ./

# Restore dependencies for the Web project (this will automatically resolve project references)
RUN dotnet restore Techcrafts.Blogging.Web/Techcrafts.Blogging.Web.csproj

# Build and publish the application
RUN dotnet publish Techcrafts.Blogging.Web/Techcrafts.Blogging.Web.csproj -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build-env /app/out .

# Configure ASP.NET Core to listen on the port Railway provides (defaulting to 3000)
ENV ASPNETCORE_HTTP_PORTS=3000
EXPOSE 3000

ENTRYPOINT ["dotnet", "Techcrafts.Blogging.Web.dll"]
