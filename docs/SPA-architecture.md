# Movie Price Comparison SPA Architecture

This document outlines the architecture for the Movie Price Comparison single-page application (SPA), focusing on component structure, data flow, and resilience strategies.

## UI Component Hierarchy

````
App
├── Header
├── Router
│   ├── MoviesPage (/)
│   │   ├── SearchBar
│   │   ├── FilterControls
│   │   ├── MovieGrid
│   │   │   ├── MovieCard (multiple)
│   │   │   │   ├── MovieThumbnail
│   │   │   │   ├── MovieInfo
│   │   │   │   └── PriceComparison
│   │   │   │
│   │   │   └── LoadingStates / ErrorStates
│   │   │
│   │   └── MovieDetailsPage (/movie/:id)
│   │       ├── MovieHeader
│   │       ├── MovieDetails
│   │       ├── PriceComparisonDetail
│   │       │   ├── ProviderPrice (Cinemaworld)
│   │       │   ├── ProviderPrice (Filmworld)
│   │       │   └── BestDealHighlight
│   │       │
│   │       └── LoadingStates / ErrorStates
│   │
│   └── Footer
│```

## Key Component Responsibilities

### 1. MovieCard
- Displays basic movie information (title, year, poster)
- Shows price comparison between the two providers
- Highlights the cheapest provider
- Handles empty/error states for missing prices
- Links to detailed movie page

### 2. PriceComparison
- Compares prices between providers
- Visually highlights the best deal
- Shows "Unavailable" for missing prices
- Implements skeleton loading state when prices are being fetched

### 3. MovieGrid
- Arranges MovieCards in a responsive grid layout
- Implements infinite scrolling or pagination
- Shows placeholder cards during loading
- Displays appropriate empty states when no movies are available

### 4. MovieDetailsPage
- Fetches and displays comprehensive movie details
- Shows detailed price comparison
- Provides fallback UI when details from one provider are unavailable
- Maintains a usable interface even when both providers fail (using cached data)

## Data Flow Architecture

````

┌─────────────────┐ ┌────────────────┐ ┌────────────────┐
│ React Query │ ◄── │ Hooks │ ◄── │ UI Layer │
│ Cache Layer │ │ │ │ (Components) │
└────────┬────────┘ └────────────────┘ └────────────────┘
│ ▲
▼ │
┌─────────────────┐ ┌────────────────┐ │
│ API Service │ ─── │ Error Handler │ ───────────┘
│ (Axios/Fetch) │ │ & Fallbacks │
└────────┬────────┘ └────────────────┘
│
▼
┌─────────────────┐
│ .NET BFF API │
└─────────────────┘

```

## UI States

For each major UI component, we implement multiple states:

1. **Loading State**
   - Skeleton placeholders for movie cards
   - Progress indicators for price comparisons
   - Animated loading indicators that don't block UI interaction

2. **Success State**
   - Complete information display
   - Price comparison with visual highlighting
   - Interactive elements fully enabled

3. **Partial Error State**
   - Display available data (e.g., show one provider's price when the other fails)
   - Visual indication of partially missing data
   - Retry mechanisms for failed data

4. **Complete Error State**
   - Friendly error message
   - Cached data display (if available)
   - Retry button
   - Alternative actions for users

## Resilience Strategies in UI

### Progressive Enhancement
- Core content and functionality work with minimal data
- Enhanced features appear when all services are available

### Graceful Degradation
- When prices from one provider are unavailable, clearly indicate this while still showing available prices
- When both providers fail, show cached results with a timestamp and retry option

### Client-side Caching
- Store fetched movie data in React Query cache
- Implement client-side persistence using localStorage for recently viewed movies
- Display cached data with "last updated" indicators when fresh data can't be fetched

## Responsive Design Approach

The UI adapts to different screen sizes using these breakpoints:

- **Mobile** (<640px): Single column layout, condensed movie cards
- **Tablet** (640px-1024px): Two-column grid, expandable movie cards
- **Desktop** (>1024px): Multi-column grid with hover effects

## Accessibility Considerations

- Color is not the only means to identify the cheapest price (icons, text)
- Loading states are announced to screen readers
- Interactive elements have appropriate focus states
- Error messages are linked to their respective controls

## Performance Optimization

- Lazy loading of images and components
- Windowing techniques for long movie lists (react-window)
- Code splitting based on routes
- Preloading data for likely user navigation paths

## User Experience Enhancements

- Instant visual feedback for user interactions
- Motion design that guides attention to price differences
- Clear visual hierarchy highlighting the most important information
- Inline help and tooltips explaining comparison features

## Implementation Guidelines

- Use Tailwind CSS for styling with consistent design tokens
- Implement components following the conventions in `.cursorrules`
- Maintain separation between UI components and data fetching logic
- Use React Query for optimal data fetching, caching, and refetching

## Error Handling Strategy

When APIs fail, the UI will:

1. **For individual movie price failures:**
   - Show "Price unavailable" for the affected provider
   - Continue showing the other provider's price
   - Add a retry mechanism
   - Display last known price (if available) with a timestamp

2. **For complete provider failure:**
   - Show all available movies from the working provider
   - Clearly indicate which provider is unavailable
   - Use cached data for the failed provider if available
   - Implement automatic and manual retry options

3. **For both providers failing:**
   - Display cached movie list if available
   - Show friendly error message with clear retry option
   - Maintain all navigation and UI interaction capabilities
   - Provide offline browsing of previously loaded content
```
