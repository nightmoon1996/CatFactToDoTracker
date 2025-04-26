# Cat Fact ToDo Tracker API

This is a simple ToDo API built with .NET Core Minimal APIs that allows users to register, log in, and manage ToDo items. Each ToDo item automatically includes a random cat fact fetched from an external API upon creation, and weather information for the ToDo's date (based on a fixed location).

## Features

- User registration and login using JWT authentication.
- Password hashing using BCrypt.
- Create ToDo items (message, date).
- Fetches and stores a cat fact with each new ToDo item.
- Fetches and includes the _current_ weather information (description and temperature) for Bangkok when retrieving items.
- View all ToDo items belonging to the authenticated user.
- Swagger/OpenAPI documentation for API testing.

## Setup and Running

1.  **Prerequisites:**
    - .NET 9 SDK (or the version specified in `TodoList.csproj`)
2.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd CatFactToDoTracker
    ```
3.  **Configure JWT Secret (Important for Production):**
    - Open `appsettings.json`.
    - Replace the placeholder value for `Jwt:Key` with a strong, unique secret key.
    - **Recommendation:** For production, use environment variables or a secrets management tool (like Azure Key Vault, AWS Secrets Manager, or .NET User Secrets for development) instead of storing the key directly in `appsettings.json`.
      - Example using Environment Variable:
        Set an environment variable named `Jwt__Key` (note the double underscore `__` for nesting) to your secret key.
4.  **Run the application:**
    ```bash
    dotnet run
    ```
5.  **Access the API:**
    - The API will typically run on `https://localhost:7xxx` and `http://localhost:5xxx` (check the console output for the exact URLs).
    - Access the Swagger UI for documentation and testing by navigating to `/swagger` in your browser (e.g., `https://localhost:7xxx/swagger`).

## API Endpoints

- **POST** `/api/auth/register`: Register a new user.
  - Body: `{ "username": "string", "password": "string" }`
- **POST** `/api/auth/login`: Log in an existing user.
  - Body: `{ "username": "string", "password": "string" }`
  - Returns: `{ "token": "string" }` (JWT)
- **POST** `/api/todos` (Requires Authentication): Create a new ToDo item.
  - Header: `Authorization: Bearer <your_jwt_token>`
  - Body: `{ "message": "string", "date": "YYYY-MM-DD" }`
  - Returns: The created ToDo item with the cat fact and current weather description for Bangkok.
- **GET** `/api/todos` (Requires Authentication): Get all ToDo items for the logged-in user.
  - Header: `Authorization: Bearer <your_jwt_token>`
  - Returns: An array of ToDo items, each including a cat fact and the current weather description for Bangkok.

## Technology Stack

- .NET 9
- ASP.NET Core Minimal APIs
- Entity Framework Core (In-Memory Provider)
- JWT Bearer Authentication
- BCrypt.Net-Next (Password Hashing)
- Swashbuckle (Swagger/OpenAPI)
