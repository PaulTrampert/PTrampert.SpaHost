# PTrampert.SpaHost
Docker image and project template for hosting single page applications. This project implements the 
[Backend for Frontend](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends) pattern and 
[Token Handler](https://levelup.gitconnected.com/secure-frontend-authorization-67ae11953723) patterns for single page applications.

## Usage as Base Image for SPA projects.
This is the primary use case for this project. The general process is this:
1. Compile your SPA project
1. Build an image using paultrampert/spahost as the base image, and copy your compiled SPA into /app/wwwroot
1. When running the image, provide necessary runtime configs (OpenID config, backend api config, redis, etc.)

A simple Dockerfile might look something like this:
```Dockerfile
FROM node-lts as build
WORKDIR /src
COPY ['package.json', 'package-lock.json', './']
RUN npm install
COPY . .
RUN npm run build

FROM paultrampert/spahost as final
COPY --from=build /src/build/ /app/wwwroot/
```
