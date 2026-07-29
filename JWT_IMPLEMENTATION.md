# JWT Refresh Token Implementation Guide

## Overview
Implemented a complete JWT authentication system with 30-day refresh token expiration for the Kono Backend API.

## Architecture

### Components Created

#### 1. **Domain Models** (`Kono.Identity/Domain`)
- **RefreshToken.cs**: EF Core entity for persisting refresh tokens
  - Tracks: UserId, Token, ExpiryDate, IsRevoked status, CreatedAt, RevokedAt
  
- **LoginHandler.cs**: DTOs for login requests
  - Properties: Email, Password
  
- **LoginResult.cs**: Response DTOs for authentication operations
  - Properties: Success, Message, AccessToken, RefreshToken, UserId

#### 2. **Services** (`Kono.Infrastructure/Services`)

**JwtTokenService.cs**
```csharp
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
```
- Generates HS256-signed JWT access tokens (15 min expiration)
- Creates cryptographically secure refresh tokens
- Validates expired tokens for refresh operations

**AuthenticationService.cs**
```csharp
public interface IAuthenticationService
{
    Task<LoginResult> LoginUserAsync(string email, string password);
    Task<LoginResult> RefreshUserTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}
```
- **LoginUserAsync**: Validates credentials and creates refresh token (30-day expiration)
- **RefreshUserTokenAsync**: Issues new tokens and revokes old refresh token
- **RevokeRefreshTokenAsync**: Revokes tokens on logout

#### 3. **Database**
- New table: `RefreshTokens`
- Migration: `20260727000000_AddRefreshTokensTable.cs`
- DbContext updated with: `public DbSet<RefreshToken> RefreshTokens`

#### 4. **API Endpoints** (`KonoApi/Program.cs`)

**POST /api/auth/login**
```json
Request:
{
  "email": "user@example.com",
  "password": "password123"
}

Response (200 OK):
{
  "success": true,
  "message": "Login successful",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abcd1234xyz...",
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**POST /api/auth/refresh**
```json
Request:
{
  "refreshToken": "abcd1234xyz..."
}

Response (200 OK):
{
  "success": true,
  "message": "Token refreshed successfully",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "new-token-xyz...",
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**POST /api/auth/logout**
```json
Request:
{
  "refreshToken": "abcd1234xyz..."
}

Response (200 OK):
{
  "message": "Logged out successfully"
}
```

## Configuration

### appsettings.json
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long-for-256-bit-hmac",
    "Issuer": "KonoApi",
    "Audience": "KonoClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  }
}
```

### Key Expiration Settings
- **Access Token**: 15 minutes (short-lived)
- **Refresh Token**: 30 days (as requested)

## Token Flow

```
1. User Login
   ├─ Validate credentials
   ├─ Generate AccessToken (15 min)
   ├─ Generate RefreshToken (30 days)
   └─ Store RefreshToken in database
   
2. Access Protected Resource
   ├─ Send request with AccessToken in Authorization header
   └─ API validates token
   
3. AccessToken Expires
   ├─ Client uses RefreshToken to get new AccessToken
   ├─ Send RefreshToken to /api/auth/refresh
   ├─ Old RefreshToken is revoked
   ├─ New AccessToken + RefreshToken issued
   └─ Store new RefreshToken
   
4. Logout
   ├─ Send RefreshToken to /api/auth/logout
   └─ RefreshToken marked as revoked
```

## Security Features

1. **Token Storage**: Refresh tokens stored in database (not in JWT)
2. **Token Revocation**: Old tokens revoked when new ones issued
3. **Expiration Validation**: Tokens checked for expiry and revocation status
4. **Secure Random Generation**: Refresh tokens use `RandomNumberGenerator` (64 bytes)
5. **HS256 Signing**: HMAC SHA-256 with 256-bit secret key
6. **Claim Validation**: User ID and Email in claims

## Usage in Controllers

```csharp
[Authorize]
[HttpGet("protected")]
public async Task<IActionResult> ProtectedEndpoint()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // Your logic here
}
```

## Deployment Checklist

- [ ] Update `Jwt:SecretKey` in production appsettings with strong random key
- [ ] Ensure HTTPS enabled in production (set `RequireHttpsMetadata = true`)
- [ ] Run migration: `dotnet ef database update`
- [ ] Test endpoints with credentials
- [ ] Configure CORS if frontend is on different domain
- [ ] Consider adding rate limiting to login endpoint
- [ ] Set up token blacklist/revocation service for additional security

## NuGet Dependencies Added

- `System.IdentityModel.Tokens.Jwt` (7.4.0)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.0)

## Notes

- The implementation follows your existing architecture patterns
- Password validation is basic (plain text comparison) - consider implementing bcrypt hashing
- User/Owner login can be separated by creating similar endpoints
- Token refresh maintains the same UserId and Email claims
