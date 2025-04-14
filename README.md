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

- ASP.NET Core 6+
- Polly for resilience patterns
- Refit for typed HTTP clients
- In-memory and distributed caching
- Serilog for logging

### Frontend (React)

- React with TypeScript
- React Query for server state
- Context API for UI state
- Tailwind CSS for styling
- Axios for API communication

## Getting Started

### Prerequisites

- .NET 6+ SDK
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

- Polly-based retry and circuit breaker patterns
- Server-side caching for reduced API dependency
- Fallback mechanisms when services are unavailable
- Graceful degradation of functionality

## Testing

### Backend Tests

```
cd server
dotnet test
```

### Frontend Tests

```
cd client
npm test
```

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

TODO: Add deployment instructions

```
docker-compose up -d
```

## License

TODO: Add deployment instructions
