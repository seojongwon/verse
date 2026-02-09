using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RetreatVerses.App.Data
{
    public static class PurposeSlug
    {
        public static string ToSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "purpose";
            }

            var normalized = value.Trim().ToLowerInvariant();
            var builder = new StringBuilder(normalized.Length);
            var previousDash = false;

            foreach (var ch in normalized)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                    previousDash = false;
                    continue;
                }

                if (ch == '-' || ch == '_' || char.IsWhiteSpace(ch))
                {
                    if (!previousDash && builder.Length > 0)
                    {
                        builder.Append('-');
                        previousDash = true;
                    }
                }
            }

            var slug = builder.ToString().Trim('-');
            if (!string.IsNullOrWhiteSpace(slug))
            {
                return slug;
            }

            return $"p-{ShortHash(normalized)}";
        }

        public static string? Resolve(string? slugOrPurpose, IEnumerable<string> purposes)
        {
            if (string.IsNullOrWhiteSpace(slugOrPurpose))
            {
                return null;
            }

            var trimmed = slugOrPurpose.Trim();
            var exact = purposes.FirstOrDefault(p => string.Equals(p, trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            var slug = trimmed.ToLowerInvariant();
            foreach (var purpose in purposes)
            {
                if (string.Equals(ToSlug(purpose), slug, StringComparison.OrdinalIgnoreCase))
                {
                    return purpose;
                }
            }

            return trimmed;
        }

        private static string ShortHash(string value)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant().Substring(0, 8);
        }
    }
}
