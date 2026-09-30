# Authentication and Authorization

## Overview

The application uses ASP.NET Core Cookie Authentication with a custom user system.

Users are stored in the application's database and authenticated using:

- Email and password
- Password hashing with `PasswordHasher<User>`
- Cookie-based authentication
- Optional TOTP-based two-factor authentication (2FA)
- Role-based authorization
- ASP.NET Core antiforgery protection for POST requests

The application currently defines two roles:

- `PublicActor`
- `ResourceProvider`

Authentication and authorization are separate concepts:

- Authentication determines who the user is.
- Authorization determines what the authenticated user is allowed to access.

ASP.NET Core creates an authenticated `ClaimsPrincipal` after successful authentication. The application stores the principal in an encrypted authentication cookie, which is used on subsequent requests. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)

---

## User Model

Users are represented by the `User` entity.

Important authentication-related properties include:

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string ActorType { get; set; } = string.Empty;

    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
}
```

Passwords are never stored as plain text. During registration, the password is hashed using:

```csharp
PasswordHasher<User>
```

During login, the supplied password is verified against the stored hash.

---

## Registration

Registration is handled by `AccountController` and `AuthService`.

The registration flow is:

```text
User
 │
 │ submits registration form
 ▼
AccountController
 │
 ▼
AuthService.RegisterAsync()
 │
 ├── Check whether email already exists
 ├── Create User
 ├── Hash password
 ├── Save user
 │
 ▼
Redirect to Login
```

A new account is created with two-factor authentication disabled by default.

Example:

```csharp
user.PasswordHash =
    _passwordHasher.HashPassword(user, model.Password);
```

The application then saves the user through the repository/unit-of-work layer.

---

## Login

Login is performed using the user's email address and password.

The flow is:

```text
Login form
    │
    ▼
AccountController
    │
    ▼
AuthService.LoginAsync()
    │
    ├── Find user by email
    │
    ├── Verify password
    │
    ├── Is 2FA enabled?
    │       │
    │       ├── No ──────► Sign in
    │       │
    │       └── Yes ─────► VerifyTwoFactor
    │
    ▼
Authenticated user
```

The password is verified using:

```csharp
_passwordHasher.VerifyHashedPassword(
    user,
    user.PasswordHash,
    model.Password);
```

If the password is incorrect, the application returns:

```text
Invalid email or password.
```

The same message is used when the email does not exist so that the application does not reveal whether a particular email address has an account.

---

## Authentication Cookie

The application uses ASP.NET Core Cookie Authentication:

```csharp
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
```

After successful authentication, the application creates a `ClaimsPrincipal` and signs the user in:

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new(ClaimTypes.Role, user.Role.ToString())
};
```

The role is stored as a claim and is subsequently available through:

```csharp
User.IsInRole("PublicActor")
```

or:

```csharp
User.IsInRole("ResourceProvider")
```

The authenticated user's ID can also be retrieved from the `NameIdentifier` claim through the project's `User.GetUserId()` extension.

The cookie authentication middleware recreates `HttpContext.User` from the authentication cookie on subsequent requests. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)

---

## Two-Factor Authentication

The application supports TOTP-based two-factor authentication.

TOTP is generated using a secret stored for the user:

```csharp
public string? TwoFactorSecret { get; set; }
```

The application uses `Otp.NET` to generate and verify TOTP codes and `QRCoder` to generate the QR code used during setup.

### Enabling 2FA

An authenticated user can access:

```text
/Account/SetupTwoFactor
```

The setup process is:

```text
Authenticated user
        │
        ▼
SetupTwoFactor
        │
        ├── Generate random secret
        │
        ├── Generate otpauth URI
        │
        ├── Generate QR code
        │
        ▼
User scans QR code
        │
        ▼
Authenticator app generates TOTP code
        │
        ▼
User enters code
        │
        ▼
Verify code
        │
        ├── Invalid → setup fails
        │
        └── Valid → save secret and enable 2FA
```

The generated secret is stored temporarily in `TempData` until the setup form is submitted.

After successful verification:

```csharp
user.TwoFactorSecret = secret;
user.TwoFactorEnabled = true;
```

---

## Two-Factor Login

If a user has 2FA enabled, entering the correct password does not immediately authenticate the user.

Instead, `LoginAsync()` returns:

```csharp
new LoginResult(
    true,
    RequiresTwoFactor: true,
    UserId: user.Id);
```

The user is then redirected to:

```text
/Account/VerifyTwoFactor
```

The user must provide a valid TOTP code.

Only after successful TOTP verification does the application call `SignInAsync()` and create the authentication cookie.

```text
Email + password
       │
       ▼
Password valid?
       │
       ▼
2FA enabled?
   │          │
  No         Yes
   │          │
   ▼          ▼
Sign in   Enter TOTP
              │
              ▼
          Code valid?
           │      │
          No     Yes
           │      │
           ▼      ▼
          Error  Sign in
```

---

## Authorization

The application uses role-based authorization.

The available roles are:

```csharp
public enum UserRole
{
    PublicActor,
    ResourceProvider
}
```

Actions that require a specific role use `[Authorize]` with the corresponding role.

For example:

```csharp
[Authorize(Roles = nameof(UserRole.PublicActor))]
public IActionResult Create()
{
    ...
}
```

A resource-provider-only action can use:

```csharp
[Authorize(Roles = nameof(UserRole.ResourceProvider))]
public IActionResult Create()
{
    ...
}
```

ASP.NET Core supports role checks through the `Authorize` attribute and `ClaimsPrincipal.IsInRole`. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/roles)

### Authorization rules

| Feature         | Required access    |
| --------------- | ------------------ |
| Home / Map      | Anonymous          |
| Register        | Anonymous          |
| Login           | Anonymous          |
| Verify 2FA      | Anonymous          |
| Account         | Authenticated      |
| Setup 2FA       | Authenticated      |
| Logout          | Authenticated      |
| View Needs      | Authenticated      |
| Create Need     | `PublicActor`      |
| View Resources  | Authenticated      |
| Create Resource | `ResourceProvider` |
| My Resources    | `ResourceProvider` |
| Matches         | Authenticated      |
| Access Denied   | Anonymous          |

The exact authorization attributes are defined on the relevant controller actions.

---

## Fallback Authorization Policy

The application uses a fallback policy:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

This means that endpoints without explicit authorization metadata require an authenticated user by default.

Public endpoints explicitly use:

```csharp
[AllowAnonymous]
```

For example:

```csharp
[AllowAnonymous]
[HttpGet]
public IActionResult Login()
{
    return View();
}
```

The login, registration, and 2FA verification pages need to be anonymous because the user is not fully authenticated when accessing them.

The fallback policy provides a default authenticated requirement while `[AllowAnonymous]` explicitly permits public access. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)

---

## Navigation vs Authorization

The navigation bar hides links based on the user's role:

```csharp
@if (User.IsInRole("PublicActor"))
{
    ...
}
```

and:

```csharp
@if (User.IsInRole("ResourceProvider"))
{
    ...
}
```

This only controls what links are displayed.

It is not a security mechanism.

The controller actions must still use authorization attributes:

```csharp
[Authorize(Roles = nameof(UserRole.PublicActor))]
```

A user can manually enter a URL without seeing the corresponding navigation link, so authorization must be enforced on the server.

---

## Antiforgery Protection

POST actions that modify application data use:

```csharp
[ValidateAntiForgeryToken]
```

For example:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    NeedCreateViewModel model,
    CancellationToken cancellationToken)
{
    ...
}
```

The Razor form automatically generates an antiforgery token when using the Form Tag Helper.

The server validates the token before executing the action.

This protects state-changing requests against Cross-Site Request Forgery (CSRF) attacks. ASP.NET Core provides built-in antiforgery support for MVC forms. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery)

---

## User ID and Claims

After authentication, the application stores the following claims:

```csharp
new(ClaimTypes.NameIdentifier, user.Id.ToString()),
new(ClaimTypes.Name, user.Username),
new(ClaimTypes.Role, user.Role.ToString())
```

These claims are used throughout the application.

For example, when creating a need:

```csharp
await _needService.CreateAsync(
    model,
    User.GetUserId(),
    cancellationToken);
```

This ensures that the created need is associated with the currently authenticated user rather than a user ID supplied by the client.

The same approach is used for resources.

---

## Logout

Logout removes the authentication cookie:

```csharp
await httpContext.SignOutAsync(
    CookieAuthenticationDefaults.AuthenticationScheme);
```

The user must authenticate again to access protected pages.

The logout endpoint is a POST action and uses antiforgery validation:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _authService.SignOutAsync(HttpContext);

    return RedirectToAction("Index", "Home");
}
```

---

## Development Users

The development database is seeded with two users so that both authorization roles can be tested.

### Public Actor

```text
Username: dev_public_actor
Email:    dev.public.actor@example.com
Password: DevPassword123!
Role:     PublicActor
```

### Resource Provider

```text
Username: dev_resource_provider
Email:    dev.resource.provider@example.com
Password: DevPassword123!
Role:     ResourceProvider
```

These accounts are intended only for local development and testing.

The passwords are hashed before being stored in the database.

---

## Testing Authorization

The two development accounts can be used to verify role restrictions.

### Test as Public Actor

Log in as:

```text
dev.public.actor@example.com
```

Expected access includes:

```text
Needs
Register Need
Matches
```

The user should not be able to access resource-provider-only functionality such as:

```text
Register Resource
My Resources
```

### Test as Resource Provider

Log in as:

```text
dev.resource.provider@example.com
```

Expected access includes:

```text
Resources
Register Resource
My Resources
```

The user should not be able to access public-actor-only functionality such as:

```text
Register Need
```

Attempting to access a forbidden action should result in the configured access-denied response.

---

## Request Pipeline

Authentication and authorization middleware are registered in the application pipeline:

```csharp
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

`UseAuthentication()` establishes the authenticated user from the authentication cookie.

`UseAuthorization()` evaluates authorization requirements such as `[Authorize]` and role restrictions.

The authentication middleware must run before authorization. [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)

---

## Security Considerations

The application currently uses an 8-hour authentication-cookie lifetime:

```csharp
options.ExpireTimeSpan = TimeSpan.FromHours(8);
```

The authentication cookie is encrypted and contains the authentication ticket and claims. The server therefore does not need to query the database on every request merely to determine the user's identity.

The application should not expose:

- `PasswordHash`
- `TwoFactorSecret`
- authentication cookies
- development credentials in production

Development seed accounts must not be used in a production environment.

All state-changing POST actions should use antiforgery validation.

Role checks must be enforced on the server even when the corresponding navigation links are hidden from the user.

---

## Authentication Components

The main authentication components are:

```text
AccountController
        │
        ▼
    AuthService
        │
        ├── UserRepository
        │
        ├── PasswordHasher<User>
        │
        └── Otp.NET
                │
                ▼
        TOTP verification

AccountController
        │
        ▼
Cookie Authentication
        │
        ▼
ClaimsPrincipal
        │
        ├── User ID
        ├── Username
        └── Role
```

The main files involved are:

```text
Controllers/
    AccountController.cs

Services/
    AuthService.cs
    UserService.cs

Services/Interfaces/
    IAuthService.cs
    IUserService.cs

Models/
    User.cs
    UserRole.cs

ViewModels/Account/
    LoginViewModel.cs
    RegisterViewModel.cs
    VerifyTwoFactorViewModel.cs
    TwoFactorSetupViewModel.cs
    AccountViewModel.cs

Views/Account/
    Login.cshtml
    Register.cshtml
    VerifyTwoFactor.cshtml
    SetupTwoFactor.cshtml
    Index.cshtml
    AccessDenied.cshtml
```

## Summary

The application uses:

```text
Authentication
    ├── Email + password
    ├── PasswordHasher<User>
    ├── Cookie authentication
    └── TOTP 2FA

Authorization
    ├── Authenticated users
    ├── PublicActor
    └── ResourceProvider

Request protection
    └── Antiforgery tokens for POST requests
```

This provides authentication, role-based authorization, optional two-factor authentication, and CSRF protection using ASP.NET Core's built-in security mechanisms.
