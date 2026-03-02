using ItchyPassword.Core.Models;
using ItchyPassword.Core.Constants;

namespace ItchyPassword.Core.Extensions;

public static class VaultItemExtensions
{
    extension(VaultItemV2 item)
    {
        /// <summary>
        /// Gets a value indicating whether the item uses legacy crypto or encoding settings.
        /// </summary>
        public bool IsLegacy()
        {
            if (item.Type == VaultItemTypeV2.Secret && item.SecretData is not null)
            {
                return item.SecretData.CryptoVersion != SecretDataConstants.LatestCryptoVersion
                    || item.SecretData.Encoding != SecretDataConstants.LatestEncoding;
            }

            if (item.Type == VaultItemTypeV2.StaticKey && item.StaticKeyData is not null)
            {
                return item.StaticKeyData.CryptoVersion != StaticKeyDataConstants.LatestCryptoVersion
                    || item.StaticKeyData.EncodingVersion != StaticKeyDataConstants.LatestEncodingVersion;
            }

            return false;
        }
    }
}
