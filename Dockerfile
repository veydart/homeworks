FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY SocialNetwork.sln .
COPY src/SocialNetwork.Domain/SocialNetwork.Domain.csproj src/SocialNetwork.Domain/
COPY src/SocialNetwork.Application/SocialNetwork.Application.csproj src/SocialNetwork.Application/
COPY src/SocialNetwork.Infrastructure/SocialNetwork.Infrastructure.csproj src/SocialNetwork.Infrastructure/
COPY src/SocialNetwork.Api/SocialNetwork.Api.csproj src/SocialNetwork.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/SocialNetwork.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SocialNetwork.Api.dll"]
