FROM node:24 AS npm-stage

WORKDIR /Mnema

COPY Mnema.Frontend/Web/package.json Mnema.Frontend/Web/package-lock.json ./
RUN npm ci

COPY Mnema.Frontend/Web ./

RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-stage

WORKDIR /Mnema

COPY Mnema.sln ./
COPY Mnema.API/Mnema.API.csproj Mnema.API/
COPY Mnema.Server/Mnema.Server.csproj Mnema.Server/
COPY Mnema.Common/Mnema.Common.csproj Mnema.Common/
COPY Mnema.Database/Mnema.Database.csproj Mnema.Database/
COPY Mnema.Models/Mnema.Models.csproj Mnema.Models/
COPY Mnema.Providers/Mnema.Providers.csproj Mnema.Providers/
COPY Mnema.Services/Mnema.Services.csproj Mnema.Services/

RUN dotnet restore Mnema.Server/Mnema.Server.csproj

COPY Mnema.API/. Mnema.API/
COPY Mnema.Server/. Mnema.Server/
COPY Mnema.Common/. Mnema.Common/
COPY Mnema.Database/. Mnema.Database/
COPY Mnema.Models/. Mnema.Models/
COPY Mnema.Providers/. Mnema.Providers/
COPY Mnema.Services/. Mnema.Services/

RUN dotnet publish Mnema.Server/Mnema.Server.csproj -c Release -o /Mnema/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=npm-stage /Mnema/dist/web/browser/ /Mnema/wwwroot
COPY --from=dotnet-stage /Mnema/publish /app

EXPOSE 8080

CMD [ "/app/Mnema.Server" ]