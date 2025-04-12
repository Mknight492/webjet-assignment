# Assumptions for Movie Price Comparison App

## API Behavior
- Both APIs return movie data in a similar JSON format
- Each movie has a unique identifier within each provider's system
- Movie IDs between providers may not be directly comparable
- APIs may have rate limits that need to be respected
- APIs may timeout or return errors occasionally
- APIs may have varying response times

## User Experience
- Users primarily want to compare prices efficiently
- Users expect to see results quickly, even if partial
- Users should be informed when data is cached or partial
- Users should be notified of any errors in a user-friendly way

## Movie Data
- Movies from both providers largely overlap but may not be identical
- Movie titles are consistent between providers
- Movie IDs follow a predictable format (e.g., "cw123" for Cinemaworld)
- Price is the primary comparison point for users

## Performance
- Application should load within 3 seconds
- API calls should timeout after a reasonable period (5 seconds)
- Caching will be acceptable to users when explained

## Security
- API token must not be exposed in client-side code
- A lightweight backend or serverless function will be used to proxy API calls
- No user personal data is being stored or transmitted

## Technical Environment
- Application will work on modern browsers (last 2 versions)
- Mobile and desktop responsive design is required
- Internet connection may be intermittent for some users

## Business Logic
- The cheapest price is always preferred regardless of provider
- All prices are in the same currency and directly comparable
- No user accounts or purchasing within the application itself 