# Architecture - Movie Price Comparison App

## Overview

This application follows a monorepo structure with a .NET Backend for Frontend (BFF) and a React frontend. The architecture is designed to handle unreliable external movie price APIs with resilience.

## Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   React Frontend (Client)                    │
└───────────────────────────────┬─────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    .NET BFF (Server)                         │
│  ┌────────────────────────────────────────────────────┐     │
│  │                API Controllers                      │     │
│  └──────────────────────────┬─────────────────────────┘     │
│                             │                                │
│  ┌──────────────────────────▼─────────────────────────┐     │
│  │               Service Layer                         │     │
│  └──────────────────────────┬─────────────────────────┘     │
│                             │                                │
│  ┌──────────────────────────▼─────────────────────────┐     │
│  │               Resilience Layer                      │     │
│  │  ┌─────────────┐ ┌──────────────┐ ┌─────────────┐  │     │
│  │  │    Retry    │ │Circuit Breaker│ │   Caching   │  │     │
│  │  └─────────────┘ └──────────────┘ └─────────────┘  │     │
│  └──────────────────────────┬─────────────────────────┘     │
└──────────────────────────────┬────────────────────────────┘
                               │
     ┌─────────────────────────┼─────────────────────────┐
     │                         │                         │
     ▼                         ▼                         ▼
┌──────────────┐       ┌──────────────┐          ┌──────────────┐
│ Cinemaworld  │       │  Filmworld   │          │  Fallback    │
│     API      │       │     API      │          │    Data      │
└──────────────┘       └──────────────┘          └──────────────┘
```

## Key Components

### 1. React Frontend (Client)

- Responsive UI built with React and TypeScript
- State management with React Query and Context API
- Styled with Tailwind CSS
- Communicates with BFF via REST API

### 2. .NET BFF (Server)

- ASP.NET Core application serving as Backend for Frontend
- Securely manages API tokens for movie providers
- Implements resilience patterns with Polly
- Aggregates and transforms data for frontend consumption
- Provides unified API for the frontend

### 3. Resilience Layer

- **Retry Mechanism**: Automatic retries with exponential backoff using Polly
- **Circuit Breaker**: Prevents cascading failures when APIs are down
- **Caching**: Reduces dependency on external APIs and improves performance
- **Fallbacks**: Provides alternative data when APIs are unavailable

### 4. External APIs

- Cinemaworld API for movie data and pricing
- Filmworld API for movie data and pricing
- Both accessed securely through the BFF

## Security Considerations

- API tokens are stored securely in the BFF (using .NET Secret Manager or environment variables)
- No sensitive credentials exposed to the client
- HTTPS for all communications

## Data Flow

1. User interacts with React frontend
2. Frontend requests data from BFF API
3. BFF fetches data from both movie providers
4. BFF applies business logic (price comparison, etc.)
5. Aggregated and processed data is returned to frontend
6. Frontend renders the data for the user

## Error Handling Strategy

- BFF provides graceful degradation when APIs fail
- Client displays appropriate error states and fallback UI
- Monitoring and logging for API failures

## Deployment Considerations

- Client and server can be deployed independently
- Docker containers for consistent environments
- CI/CD pipeline for automated testing and deployment
