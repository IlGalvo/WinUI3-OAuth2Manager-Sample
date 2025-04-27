# WinUI3 OAuth2Manager Client/Server Sample 

## Overview

This is a full-stack example demonstrating how to integrate the **Windows App SDK**’s **OAuth2Manager** in a **WinUI 3** client alongside a minimal **ASP.NET Core** authorization server powered by **OpenIddict**. The **Client** project shows how to launch the system browser to perform the Authorization Code + PKCE flow and receive the callback via protocol activation, while the **Server** project implements an in-memory OpenIddict server exposing `/authorize` and `/token` endpoints for rapid local testing.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Prerequisites

- **Windows 10 (1809+) or Windows 11** with the **Windows App SDK** installed (v1.7 or later).
- **Visual Studio 2022** with the **.NET 8+** and **Windows App SDK** workloads.
- **.NET 8 SDK** or newer for building both client and server.

## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/IlGalvo/WinUI3OAuth2ManagerSample.git
   cd WinUI3OAuth2ManagerSample
   ```
2. **Run the Server**
   - Open `Server/Server.sln` in Visual Studio.
   - Set **Server** as the startup project and press **F5**.
   - The server will start on and host the OpenIddict endpoints.
3. **Run the Client**
   - Open `Client/Client.sln` in Visual Studio.
   - Set **Client** as the startup project and press **F5**.
   - Click the **Authorize** button to launch the browser and start the OAuth 2.0 flow.

## Project Structure

```
WinUI3OAuth2ManagerSample/
├─ Client/
│  └─ Client/         ← WinUI 3 desktop app using OAuth2Manager
└─ Server/
   └─ Server/         ← ASP.NET Core Web API with OpenIddict (in-memory)
```

- **Client/Client**: Implements `OAuth2Manager.RequestAuthWithParamsAsync` to open the browser and `ProtocolActivation` to capture the redirect URI. Uses `ActivationRegistrationManager` to register custom URI schemes when unpackaged.
- **Server/Server**: Configures OpenIddict for Authorization Code + PKCE, in-memory stores, and ephemeral keys. Exposes `/connect/authorize` and `/connect/token` endpoints.

## Usage

1. In the **Client** app, click **Authorize**. The system browser navigates to auth URI.
2. The **Server** prompts for user consent and issues an authorization code.
3. The **Client** intercepts the custom URI callback via protocol activation.

## Contributing

Contributions are welcome! Please open issues and pull requests to:

- Improve documentation and add detailed README instructions.
- Enhance error handling and implement token management in the client.
- Extend the server with refresh tokens, user management, or external identity providers.

## License

This sample is licensed under the See [LICENSE](LICENSE.md) for details.

