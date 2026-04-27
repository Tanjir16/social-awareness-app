FROM node:20 AS frontend
WORKDIR /client
COPY SocialMediaApp/client/package*.json ./
RUN npm install
COPY SocialMediaApp/client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
COPY --from=frontend /client/dist ./SocialMediaApp/wwwroot/
RUN dotnet publish SocialMediaApp/SocialMediaApp.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "SocialMediaApp.dll"]
