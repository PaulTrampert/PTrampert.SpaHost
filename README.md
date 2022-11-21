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
| ForwardedHeadersConfig__AllowedHosts__[0-n] | string[] | '*' | Array of allowed hosts to be passed in x-forwarded-host |
| ForwardedHeadersConfig__ForwardedForHeaderName | string | 'X-Forwarded-For' | The name of the forwarded for header |
| ForwardedHeadersConfig__ForwardedHostHeaderName | string | 'X-Forwarded-Host' | The name of the forwarded host header |
| ForwardedHeadersConfig__ForwardedProtoHeaderName | string | 'X-Forwarded-Proto' | The name of the forwarded proto header |
| ForwardedHeadersConfig__ForwardLimit | int | 1 | The max number of reverse proxies to respect |
| ForwardedHeadersConfig__KnownNetworks__[0-n] | array of [CIDR](https://www.digitalocean.com/community/tutorials/understanding-ip-addresses-subnets-and-cidr-notation-for-networking) network strings | "127.0.0.0/8", "172.18.0.0/16", "172.17.0.0/16", "10.0.0.0/16" | The networks trusted reverse proxies may be coming from |
| ForwardedHeadersConfig__KnownProxies__[0-n] | array of IP Addresses | [] | The exact addresses of known proxies |
| ForwardedHeadersConfig__OriginalForHeaderName | string | 'X-Original-For' | The name of the original for header |
| ForwardedHeadersConfig__OriginalHostHeaderName | string | 'X-Original-Host' | The name of the original host header |
| ForwardedHeadersConfig__OriginalProtoHeaderName | string | 'X-Original-Proto' | The name of the original proto header |
| ForwardedHeadersConfig__RequireHeaderSymmetry | boolean | false | Require the number of header values to be in sync between the different headers being processed. |

### Antiforgery
Antiforgery protection is enabled by default. This means that for requests to authenticated routes, your client application will either need to supply an antiforgery token via a header or a form field. The antiforgery token can be obtained via
[`GET /antiforgery`](#get-antiforgery).

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| Antiforgery__EnableProtection | boolean | true | Enable antiforgery protection |
| Antiforgery__HeaderName | string | X-XSRF-Token | The header name to look for an antiforgery token in |
| Antiforgery__FieldName | string | antiforgeryToken | The form field to look for an antiforgery token in |

### RedisConfig
Redis is used to store DataProtection keys in clustered scenarios. For production, the redis used should be configured to persist storage.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| RedisConfig__UseForDataProtection | boolean | false | When running replicas, set to true to use Redis to share the encryption keys used for encrypting session cookies |
| RedisConfig__DataProtectionConnectionString | string | redis:6379 | The Redis connection string to be used for key storage. |

### ApiProxy
This configuration section configures proxies to the back-end api's your SPA requires. See https://github.com/PaulTrampert/PTrampert.ApiProxy#readme for details on how to configure your api proxies. All api routes will be exposed to your SPA under the `/api/` base path.

### Serilog
This project uses Serilog by default. See [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration#serilogsettingsconfiguration--) for detailed configuration instructions.

## Utility Routes
### `POST /login`
Posting to this route will issues an authentication challenge (aka redirect you to the identity provider). For best results, make sure you use a form POST to this route.

### `POST /logout`
This route will log you out of the application and redirect you to your identity provider for logout. For best results, make sure you use a form POST to this route.

### `GET /antiforgery`
This route will set a cookie named `XSRF-TOKEN` with an antiforgery token. This token should be supplied in fetch requests via the configured `Antiforgery__HeaderName` or in form submissions via a hidden input with the name configured in `Antiforgery__FieldName`.

### `GET /userinfo`
When authenticated, this route will return an array of claims assigned to the current user. The exact list of claims will depend on your identity provider and configured scopes.

Example Response:
```json
[
    {
        "type": "auth_time",
        "value": "1668133569",
        "valueType": "http://www.w3.org/2001/XMLSchema#integer"
    },
    {
        "type": "jti",
        "value": "8211cc80-6550-4752-96af-a32e8f06cc96",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "sub",
        "value": "e10ab9f6-d04b-45f9-8a31-7d63312968f2",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "typ",
        "value": "ID",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "session_state",
        "value": "b8df9292-f1ac-40fb-99f2-80c5600492b2",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "s_hash",
        "value": "VOplksQDiuudOUgBBQYjUA",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "sid",
        "value": "b8df9292-f1ac-40fb-99f2-80c5600492b2",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "name",
        "value": "Test Testerson",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "preferred_username",
        "value": "test",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "given_name",
        "value": "Test",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    },
    {
        "type": "family_name",
        "value": "Testerson",
        "valueType": "http://www.w3.org/2001/XMLSchema#string"
    }
]
```
