# Movie Price Comparison API Documentation

This document describes the external APIs used by the application and their response formats.

## External APIs

The application integrates with two movie data providers: Cinemaworld and Filmworld.

### Base URLs

- Cinemaworld: `https://webjetapitest.azurewebsites.net/api/cinemaworld/`
- Filmworld: `https://webjetapitest.azurewebsites.net/api/filmworld/`

### Authentication

All requests require an API key sent in the `x-api-key` header.

## Endpoints

### Get Movies List

Retrieves a list of available movies from a provider.

**Endpoint:**

```
GET /movies
```

**Example Response (Filmworld):**

```json
{
  "Movies": [
    {
      "Title": "Star Wars: Episode IV - A New Hope",
      "Year": "1977",
      "ID": "fw0076759",
      "Type": "movie",
      "Poster": "https://m.media-amazon.com/images/M/MV5BOTIyMDY2NGQtOGJjNi00OTk4LWFhMDgtYmE3M2NiYzM0YTVmXkEyXkFqcGdeQXVyNTU1NTfwOTk@._V1_SX300.jpg"
    },
    {
      "Title": "Star Wars: Episode V - The Empire Strikes Back",
      "Year": "1980",
      "ID": "fw0080684",
      "Type": "movie",
      "Poster": "https://m.media-amazon.com/images/M/MV5BMjE2MzQwMTgxN15BMl5BanBnXkFtZTfwMDQzNjk2OQ@@._V1_SX300.jpg"
    }
  ]
}
```

**Response Structure:**

- `Movies`: Array of movie objects
  - `Title`: Movie title (string)
  - `Year`: Release year (string)
  - `ID`: Unique identifier (string with provider prefix, e.g., "fw" for Filmworld)
  - `Type`: Content type, usually "movie" (string)
  - `Poster`: URL to movie poster image (string)

### Get Movie Details

Retrieves detailed information about a specific movie, including its price.

**Endpoint:**

```
GET /movie/{ID}
```

**Example Response (Filmworld):**

```json
{
  "Title": "Star Wars: Episode IV - A New Hope",
  "Year": "1977",
  "Rated": "PG",
  "Released": "25 May 1977",
  "Runtime": "121 min",
  "Genre": "Action, Adventure, Fantasy",
  "Director": "George Lucas",
  "Writer": "George Lucas",
  "Actors": "Mark Hamill, Harrison Ford, Carrie Fisher, Peter Cushing",
  "Plot": "Luke Skywalker joins forces with a Jedi Knight, a cocky pilot, a wookiee and two droids to save the galaxy from the Empire's world-destroying battle-station, while also attempting to rescue Princess Leia from the evil Darth Vader.",
  "Language": "English",
  "Country": "USA",
  "Poster": "https://m.media-amazon.com/images/M/MV5BOTIyMDY2NGQtOGJjNi00OTk4LWFhMDgtYmE3M2NiYzM0YTVmXkEyXkFqcGdeQXVyNTU1NTfwOTk@._V1_SX300.jpg",
  "Metascore": "92",
  "Rating": "8.7",
  "Votes": "915,459",
  "ID": "fw0076759",
  "Type": "movie",
  "Price": "29.5"
}
```

**Response Structure:**

- `Title`: Movie title (string)
- `Year`: Release year (string)
- `Rated`: Content rating (string)
- `Released`: Release date (string)
- `Runtime`: Movie duration (string)
- `Genre`: Movie genres (string)
- `Director`: Director name(s) (string)
- `Writer`: Writer name(s) (string)
- `Actors`: Main cast (string)
- `Plot`: Movie synopsis (string)
- `Language`: Movie language(s) (string)
- `Country`: Production country (string)
- `Poster`: URL to movie poster image (string)
- `Metascore`: Metascore rating (string)
- `Rating`: User rating (string)
- `Votes`: Number of user votes (string)
- `ID`: Unique identifier (string with provider prefix)
- `Type`: Content type (string)
- `Price`: Movie price (string)

## API Characteristics

- **Reliability**: Both APIs are known to be occasionally unavailable or slow.
- **Response Time**: Variable, can sometimes exceed 5 seconds.
- **Rate Limiting**: Unknown but should be assumed to exist.
- **Consistency**: Movie IDs are provider-specific and not directly comparable between providers.
