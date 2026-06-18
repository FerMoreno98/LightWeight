

using LightWeight.Auth.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Entities;

public sealed class DeviceToken : Entity<Guid>
{
            
        internal DeviceToken
        (
            Guid id,
            Guid userId, 
            string deviceIdentifier,
            string deviceName, 
            string platform, 
            DateTime lastSeenAt, 
            DateTime createdAt, 
            DateTime? revokedAt
        ) : base(id)
        {
            UserId = userId;
            DeviceIdentifier = deviceIdentifier;
            DeviceName = deviceName;
            Platform = platform;
            LastSeenAt = lastSeenAt;
            CreatedAt = createdAt;
            RevokedAt = revokedAt;
        }
        /// <summary>
        /// reference to user
        /// </summary>
        public Guid UserId {get;private set;}
        /// <summary>
        /// Identifier of the device (it will be created by the front-end)
        /// </summary>
        public string DeviceIdentifier {get; private set;}
        /// <summary>
        /// Name of the device
        /// </summary>
        public string DeviceName{get; private set;}
        /// <summary>
        /// Device platform (IOS, Android)
        /// </summary>
        public string Platform{get; private set;}
        /// <summary>
        /// Date in which user was logged with this device
        /// </summary>
        public DateTime LastSeenAt{get; private set;}
        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedAt{get; private set;}
        /// <summary>
        /// Date in which device was revoked
        /// </summary>
        public DateTime? RevokedAt{get; private set;}
        private readonly List<RefreshToken> _refreshTokens = new();
        /// <summary>
        /// List of refresh tokens
        /// </summary>
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        /// <summary>
        /// determines if token is active
        /// </summary>
        public bool IsActive => RevokedAt is null;
        /// <summary>
        /// emits a token and revoke the older one in case it exists
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public RefreshToken IssueRefreshToken(string ip, DateTime now)
        {
            RefreshToken? lastActive = _refreshTokens.FirstOrDefault(r => r.RevokedAt is null);
            var token = RefreshToken.Create(this.Id, ip,now);
            lastActive?.Revoke(now);
            lastActive?.ReplaceToken(token.Token);
            _refreshTokens.Add(token);
            return token;
        }
        /// <summary>
        /// updates the date of the last log in of a device
        /// </summary>
        /// <param name="now"></param>
        public void UpdateLastSeen(DateTime now)
        {
            LastSeenAt=now;
        }
        /// <summary>
        /// Validates that refresh token is null, revoked or expired
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="now"></param>
        /// <exception cref="StolenRefreshTokenException"></exception>
        /// <exception cref="RevokedRefreshTokenException"></exception>
        /// <exception cref="InvalidRefreshTokenException"></exception>
        public void ValidateRefreshToken(string Token, DateTime now)
        {
            RefreshToken? refreshToken = RefreshTokens.FirstOrDefault(r=> r.Token == Token);
            if (refreshToken?.RevokedAt is not null)
            {
                // Si alguien intenta usar un token revocado que YA fue reemplazado,
                // significa que están usando un token antiguo → posible robo
                if (refreshToken.ReplacedTokenBy is not null)
                    // Le paso el userId para que a la hora de propagar la excepcion con el middleware
                    // vaya al command de logout con el dato que necesita
                    throw new StolenRefreshTokenException(UserId); // invalidar toda la sesión

                // Revocado manualmente (logout) → relogin normal
                throw new RevokedRefreshTokenException();
            }
            if(refreshToken is null ||  refreshToken.ExpiresAt < now)
            {
                throw new InvalidRefreshTokenException();
            }
        }

        public void RevokeRefreshToken(string token, DateTime now)
        {
            RefreshToken? refreshToken = 
            RefreshTokens.FirstOrDefault(r=> r.Token == token) ?? throw new RefreshTokenNotFoundException();
            refreshToken.Revoke(now);
        }
}