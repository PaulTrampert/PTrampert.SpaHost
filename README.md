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

## Configuration
Configuration is read from the following sources, in order of lowest to highest precedence:
* /app/appsettings.json
* /app/appsettings.$ASPNETCORE_ENVIRONMENT.json
* Custom-specified config files listed in $SPAHOST_ADDITIONAL_APPSETTINGS
* /run/secrets/*
* Environment variables
* Command Line Parameters

Json config keys can be converted to environment variable names in the following manner:
| Json | Environment Variable | Command-Line Parameter |
|------|----------------------|------------------------|
| `{ "foo": { "bar": "value" } }` | `foo__bar=value` | `--foo:bar value` |
For more information, see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0

Files found in `/run/secrets/*` will be treated the same as environment variables. For example, if you have a file, `/run/secrets/foo__bar` containing the content `value`, it would be equivalent to the json config `{ "foo": { "bar": "value" } }`.

### AuthConfig
Values here are used to configure authentication. If no values are provided, the app will not have any authentication.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| AuthConfig__RequireForStaticFiles | boolean | false | Require authentication for files in wwwroot. This is useful if your entire SPA requires authentication. |
| AuthConfig__CookieConfig__ExpireTimeSpan | hh:mm:ss | 00:30:00 | How long a user's session lasts before needing to re-authenticate. |
| AuthConfig__CookieConfig__SlidingExpiration | boolean | true | Should the session expiration be reset on user actions? |
| AuthConfig__OidcConfig__Authority | url | null | The base path of the OpenId Connect server to use for authentication |
| AuthConfig__OidcConfig__ClientId | string | null | The OpenId Connect client id for this client. |
| AuthConfig__OidcConfig__ClientSecret | string | null | The OpenId Connect client secret for this client. |
| AuthConfig__OidcConfig__Scopes__[0-n] | string[] | null | The scopes this client requires. Must always include `openid`. Should also include `offline_access` to make sure access tokens can be refreshed. |

### ForwardedHeadersConfig
These configuration values are used to allow your app to play nice with a reverse proxy (e.g. Traefik as an ingress proxy for docker swarm). Sensible defaults for deployment in docker swarm are pre-configured. Most of these values are passed, unmodified, to [ForwardedHeadersOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.forwardedheadersoptions?view=aspnetcore-6.0)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| ForwardedHeadersConfig__AllowedHosts_[0-n] | string[] | '*' | Array of allowed hosts to be passed in x-forwarded-host |
| ForwardedHeadersConfig__ForwardedForHeaderName | string | 'X-Forwarded-For' | The name of the forwarded for header |
| ForwardedHeadersConfig__ForwardedHostHeaderName | string | 'X-Forwarded-Host' | The name of the forwarded host header |
| ForwardedHeadersConfig__ForwardedProtoHeaderName | string | 'X-Forwarded-Proto' | The name of the forwarded proto header |
| ForwardedHeadersConfig__ForwardLimit | int | 1 | The max number of reverse proxies to respect |
| ForwardedHeadersConfig__KnownNetworks | array of [CIDR](https://www.digitalocean.com/community/tutorials/understanding-ip-addresses-subnets-and-cidr-notation-for-networking) network strings | "127.0.0.0/8", "172.18.0.0/16", "172.17.0.0/16", "10.0.0.0/16" | The networks trusted reverse proxies may be coming from |

