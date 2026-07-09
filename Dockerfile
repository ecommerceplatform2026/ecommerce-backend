FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# layer cache: restore only when project files change
COPY Ecommerce/Domain/*.csproj Domain/
COPY Ecommerce/Application/*.csproj Application/
COPY Ecommerce/Infrastructure/*.csproj Infrastructure/
COPY Ecommerce/Presentation/*.csproj Presentation/
RUN dotnet restore Presentation/Presentation.csproj

COPY Ecommerce/ .
RUN dotnet publish Presentation/Presentation.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Presentation.dll"]
