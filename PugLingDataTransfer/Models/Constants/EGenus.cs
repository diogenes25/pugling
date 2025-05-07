using System.Text.Json.Serialization;

namespace PugLingDataTransfer.Models.Constants
{
    /// <summary>
    /// Represents the grammatical gender enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))] // Enables JSON string conversion for enums
    public enum EGenus
    {
        /// <summary>
        /// Represents the masculine gender.
        /// </summary>
        Masculine = 1,

        /// <summary>
        /// Represents the feminine gender.
        /// </summary>
        Feminine = 2,

        /// <summary>
        /// Represents the neuter gender.
        /// </summary>
        Neuter = 3,

        /// <summary>
        /// Represents an unset or undefined gender.
        /// </summary>
        NotSet = 0
    }
}