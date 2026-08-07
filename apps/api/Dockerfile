FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OrganizationalSingularity.sln ./
COPY src/OrganizationalSingularity.Api/OrganizationalSingularity.Api.csproj src/OrganizationalSingularity.Api/
COPY src/OrganizationalSingularity.Domain/OrganizationalSingularity.Domain.csproj src/OrganizationalSingularity.Domain/
COPY src/OrganizationalSingularity.Infrastructure/OrganizationalSingularity.Infrastructure.csproj src/OrganizationalSingularity.Infrastructure/
RUN dotnet restore src/OrganizationalSingularity.Api/OrganizationalSingularity.Api.csproj

COPY src/ src/
RUN dotnet publish src/OrganizationalSingularity.Api/OrganizationalSingularity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN useradd --uid 1000 --create-home appuser
USER appuser
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OrganizationalSingularity.Api.dll"]
