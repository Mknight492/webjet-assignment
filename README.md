# Movie Price Comparison App

A web application that allows users to compare movie prices from Cinemaworld and Filmworld providers, displaying the cheapest option.

## Project Overview

This application follows a monorepo structure with a .NET Backend for Frontend (BFF) and a React frontend. It's designed to be resilient against service disruptions, ensuring that users can continue to compare movie prices even when one or both of the provider APIs are experiencing issues.

## Key Features

- Compare movie prices across multiple providers
- View detailed movie information
- Resilient against API failures
- Fast and responsive user interface
- Secure handling of API tokens

## Project Documentation

- [Development Plan](./docs/plan.md) - Outlines the phases and approach for building the application
- [Architecture](./docs/architecture.md) - Details the technical architecture and components
- [Assumptions](./docs/assumptions.md) - Lists the assumptions made during development

## Technology Stack

### Backend (.NET BFF)

- ASP.NET Core 8
- .Net Resilience library for retry and circuit breaker patterns
- In-memory caching
- Server sent events for streaming slow price updates to the client

### Frontend (React)

- React with TypeScript
- Tailwind CSS for styling

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js (v14+)
- npm or yarn

### Installation

1. Clone the repository

2. Set up backend

   ```
   cd server
   dotnet restore
   ```

3. Configure API token

   ```
   dotnet user-secrets init
   dotnet user-secrets set "MovieApi:ApiKey" "your-api-token"
   dotnet user-secrets set "TMDB:ApiKey" "your-api-token"
   ```

4. Set up frontend

   ```
   cd client
   npm install
   ```

5. Start the backend server

   ```
   cd server
   dotnet run
   ```

6. Start the frontend development server

   ```
   cd client
   npm start
   ```

7. Open your browser to http://localhost:3000

## Development Approach

The application is built with resilience in mind, using a Backend for Frontend (BFF) pattern to:

- Securely manage API tokens
- Implement resilience patterns on the server side
- Aggregate data from multiple sources
- Provide consistent data to the frontend
- Shield the frontend from API complexities

Key resilience strategies include:

- .Net Resilience library for retry and circuit breaker patterns
- Server-side caching for reduced API dependency
- Graceful degradation of functionality

## Testing

TODO

## Building for Production

### Build Backend

```
cd server
dotnet publish -c Release
```

### Build Frontend

```
cd client
npm run build
```

## Deployment

CICD pipeline deploys to Azure Static Web Apps and Azure App Services on merge to main

## License

TODO: Add deployment instructions
