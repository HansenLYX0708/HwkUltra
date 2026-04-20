namespace HWKUltra.UI.Services
{
    /// <summary>
    /// User roles for access control
    /// </summary>
    public enum UserRole
    {
        None,
        User,
        Admin
    }

    /// <summary>
    /// Authentication modes
    /// </summary>
    public enum AuthMode
    {
        Offline,
        Online
    }

    /// <summary>
    /// Represents an authenticated user session
    /// </summary>
    public class UserSession
    {
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.None;
        public AuthMode Mode { get; set; } = AuthMode.Offline;
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Authentication service — handles offline built-in accounts and online 3rd-party auth interface
    /// </summary>
    public class AuthService
    {
        private static readonly Dictionary<string, (string Password, UserRole Role)> _offlineAccounts = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HWK", ("123", UserRole.Admin) },
            { "User", ("123", UserRole.User) }
        };

        /// <summary>
        /// Current authenticated user session (null = not logged in)
        /// </summary>
        public UserSession? CurrentSession { get; private set; }

        /// <summary>
        /// Whether a user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => CurrentSession != null;

        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        public event EventHandler<UserSession?>? AuthStateChanged;

        /// <summary>
        /// Authenticate using offline built-in accounts
        /// </summary>
        public (bool Success, string Message) LoginOffline(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty");

            if (!_offlineAccounts.TryGetValue(username, out var account))
                return (false, "Invalid username or password");

            if (account.Password != password)
                return (false, "Invalid username or password");

            CurrentSession = new UserSession
            {
                Username = username,
                Role = account.Role,
                Mode = AuthMode.Offline,
                LoginTime = DateTime.Now
            };

            AuthStateChanged?.Invoke(this, CurrentSession);
            return (true, $"Welcome, {username}");
        }

        /// <summary>
        /// Authenticate using online 3rd-party service (interface placeholder)
        /// </summary>
        public async Task<(bool Success, string Message)> LoginOnlineAsync(string username, string token)
        {
            // TODO: Integrate 3rd-party authentication service
            // Example: call external API with username + token, verify response,
            // extract role from claims, create UserSession

            await Task.Delay(100); // Simulate network call
            return (false, "Online authentication is not yet implemented");
        }

        /// <summary>
        /// Log out current user
        /// </summary>
        public void Logout()
        {
            CurrentSession = null;
            AuthStateChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Check if current user has the required role
        /// </summary>
        public bool HasRole(UserRole requiredRole)
        {
            if (CurrentSession == null) return false;
            return CurrentSession.Role >= requiredRole;
        }
    }
}
