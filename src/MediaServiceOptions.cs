using System;
using System.Collections.Generic;

namespace MediaServices
{
    /// <summary>
    /// Configuration settings for accessing Media Services. The values for this
    /// can be retrieved using the az command line:
    /// <code>az ams account sp create --account-name [account] --resource-group [group]</code>
    /// </summary>
    public class MediaServiceOptions : IEquatable<MediaServiceOptions>
    {
        public string AadTenantId { get; set; }

        public Uri ArmEndpoint { get; set; } = new Uri("https://management.azure.com/");

        public string AadClientId { get; set; }

        public string AadSecret { get; set; }

        public string SubscriptionId { get; set; }

        public string ResourceGroup { get; set; }

        public string AccountName { get; set; }

        #region Equality Checks

        /// <summary>Determines whether the specified object is equal to this instance.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MediaServiceOptions);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(MediaServiceOptions other)
        {
            return other != null &&
                   this.AadTenantId == other.AadTenantId &&
                   EqualityComparer<Uri>.Default.Equals(this.ArmEndpoint, other.ArmEndpoint) &&
                   this.AadClientId == other.AadClientId &&
                   this.AadSecret == other.AadSecret &&
                   this.SubscriptionId == other.SubscriptionId &&
                   this.ResourceGroup == other.ResourceGroup &&
                   this.AccountName == other.AccountName;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.AadTenantId,
                this.ArmEndpoint,
                this.AadClientId,
                this.AadSecret,
                this.SubscriptionId,
                this.ResourceGroup,
                this.AccountName);
        }

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="options1">The first instance to be compared.</param>
        /// <param name="options2">The second instance to be compared.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MediaServiceOptions options1, MediaServiceOptions options2)
        {
            return EqualityComparer<MediaServiceOptions>.Default.Equals(options1, options2);
        }

        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="options1">The first instance to be compared.</param>
        /// <param name="options2">The second instance to be compared.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MediaServiceOptions options1, MediaServiceOptions options2)
        {
            return !(options1 == options2);
        }
        #endregion
    }
}
