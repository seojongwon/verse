using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace RetreatVerses.App.Data
{
    public sealed class JsonDataStore : IDataStore
    {
        private const string GroupsFileName = "groups.json";
        private const string VersesFileName = "verses.json";
        private const string RegistrationsFileName = "registrations.json";
        private const string PurposesFileName = "purposes.json";
        private const string DefaultMealPurpose = "식사용";
        private const string DefaultSnackPurpose = "간식용";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new VerseJsonConverter() }
        };

        private readonly string _dataRoot;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        public JsonDataStore(IWebHostEnvironment env, IConfiguration configuration)
        {
            _dataRoot = ResolveDataRoot(env, configuration["DataRoot"]);
            Directory.CreateDirectory(_dataRoot);
        }

        private static string ResolveDataRoot(IWebHostEnvironment env, string? configuredRoot)
        {
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                return configuredRoot;
            }

            var isAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            var home = Environment.GetEnvironmentVariable("HOME");
            if (isAzure && !string.IsNullOrWhiteSpace(home))
            {
                return Path.Combine(home, "site", "data", "App_Data");
            }

            return Path.Combine(env.ContentRootPath, "App_Data");
        }

        public async Task<IReadOnlyList<Group>> GetGroupsAsync()
        {
            return await ReadListAsync<Group>(GroupsFileName);
        }

        public async Task<Group> AddGroupAsync(string name, string password)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Group name is required.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Group password is required.", nameof(password));
            }

            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                var group = new Group { Id = Guid.NewGuid(), Name = name.Trim() };
                group.PasswordHash = HashPassword(group.Id, password);
                groups.Add(group);
                await WriteListInternalAsync(GroupsFileName, groups);
                return group;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> UpdateGroupAsync(Guid id, string name, string? password)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                var target = groups.FirstOrDefault(g => g.Id == id);
                if (target == null)
                {
                    return false;
                }

                target.Name = name.Trim();
                if (!string.IsNullOrWhiteSpace(password))
                {
                    target.PasswordHash = HashPassword(target.Id, password);
                }
                await WriteListInternalAsync(GroupsFileName, groups);
                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> DeleteGroupAsync(Guid id)
        {
            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                var target = groups.FirstOrDefault(g => g.Id == id);
                if (target == null)
                {
                    return false;
                }

                groups.Remove(target);
                await WriteListInternalAsync(GroupsFileName, groups);

                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                registrations.RemoveAll(r => r.GroupId == id);
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> VerifyGroupPasswordAsync(Guid groupId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var groups = await ReadListAsync<Group>(GroupsFileName);
            var group = groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null || string.IsNullOrWhiteSpace(group.PasswordHash))
            {
                return false;
            }

            var hashed = HashPassword(group.Id, password);
            return string.Equals(group.PasswordHash, hashed, StringComparison.Ordinal);
        }

        public async Task<int> DeleteGroupsAsync(IEnumerable<Guid> ids)
        {
            var idSet = new HashSet<Guid>(ids);
            if (idSet.Count == 0)
            {
                return 0;
            }

            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                var removedCount = groups.RemoveAll(g => idSet.Contains(g.Id));
                if (removedCount == 0)
                {
                    return 0;
                }

                await WriteListInternalAsync(GroupsFileName, groups);

                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                registrations.RemoveAll(r => idSet.Contains(r.GroupId));
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return removedCount;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<int> DeleteAllGroupsAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                var count = groups.Count;
                if (count == 0)
                {
                    return 0;
                }

                await WriteListInternalAsync(GroupsFileName, new List<Group>());
                await WriteListInternalAsync(RegistrationsFileName, new List<Registration>());
                return count;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<IReadOnlyList<Verse>> GetVersesAsync()
        {
            return await ReadListAsync<Verse>(VersesFileName);
        }

        public async Task<Verse> AddVerseAsync(string text, string type)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Verse text is required.", nameof(text));
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Verse type is required.", nameof(type));
            }

            await _mutex.WaitAsync();
            try
            {
                await EnsureDefaultPurposesAsync();
                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                var verse = new Verse { Id = Guid.NewGuid(), Text = text.Trim(), Type = type.Trim() };
                verses.Add(verse);
                await WriteListInternalAsync(VersesFileName, verses);
                return verse;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> UpdateVerseAsync(Guid id, string text, string type)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            await _mutex.WaitAsync();
            try
            {
                await EnsureDefaultPurposesAsync();
                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                var target = verses.FirstOrDefault(v => v.Id == id);
                if (target == null)
                {
                    return false;
                }

                target.Text = text.Trim();
                target.Type = type.Trim();
                await WriteListInternalAsync(VersesFileName, verses);
                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> DeleteVerseAsync(Guid id)
        {
            await _mutex.WaitAsync();
            try
            {
                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                var target = verses.FirstOrDefault(v => v.Id == id);
                if (target == null)
                {
                    return false;
                }

                verses.Remove(target);
                await WriteListInternalAsync(VersesFileName, verses);

                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                registrations.RemoveAll(r => r.VerseId == id);
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<int> DeleteVersesAsync(IEnumerable<Guid> ids)
        {
            var idSet = new HashSet<Guid>(ids);
            if (idSet.Count == 0)
            {
                return 0;
            }

            await _mutex.WaitAsync();
            try
            {
                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                var removedCount = verses.RemoveAll(v => idSet.Contains(v.Id));
                if (removedCount == 0)
                {
                    return 0;
                }

                await WriteListInternalAsync(VersesFileName, verses);

                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                registrations.RemoveAll(r => idSet.Contains(r.VerseId));
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return removedCount;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<int> DeleteAllVersesAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                var count = verses.Count;
                if (count == 0)
                {
                    return 0;
                }

                await WriteListInternalAsync(VersesFileName, new List<Verse>());
                await WriteListInternalAsync(RegistrationsFileName, new List<Registration>());
                return count;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<IReadOnlyList<Registration>> GetRegistrationsAsync()
        {
            return await ReadListAsync<Registration>(RegistrationsFileName);
        }

        public async Task<IReadOnlyList<Registration>> GetRegistrationsForGroupAsync(Guid groupId)
        {
            var registrations = await ReadListAsync<Registration>(RegistrationsFileName);
            return registrations.Where(r => r.GroupId == groupId).ToList();
        }

        public async Task<OperationResult> RegisterVerseAsync(Guid groupId, Guid verseId)
        {
            await _mutex.WaitAsync();
            try
            {
                var groups = await ReadListInternalAsync<Group>(GroupsFileName);
                if (groups.All(g => g.Id != groupId))
                {
                    return new OperationResult(false, "조 정보를 찾을 수 없습니다.");
                }

                var verses = await ReadListInternalAsync<Verse>(VersesFileName);
                if (verses.All(v => v.Id != verseId))
                {
                    return new OperationResult(false, "말씀 정보를 찾을 수 없습니다.");
                }

                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                if (registrations.Any(r => r.GroupId == groupId && r.VerseId == verseId))
                {
                    return new OperationResult(false, "이미 등록된 말씀입니다.");
                }

                registrations.Add(new Registration
                {
                    GroupId = groupId,
                    VerseId = verseId,
                    RegisteredAt = DateTime.UtcNow
                });

                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return new OperationResult(true, "말씀이 등록되었습니다.");
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<OperationResult> UseVerseAsync(Guid groupId, Guid verseId)
        {
            await _mutex.WaitAsync();
            try
            {
                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                var target = registrations.FirstOrDefault(r => r.GroupId == groupId && r.VerseId == verseId);
                if (target == null)
                {
                    return new OperationResult(false, "등록된 말씀을 찾을 수 없습니다.");
                }

                if (target.UsedAt.HasValue)
                {
                    return new OperationResult(false, "이미 사용된 말씀입니다.");
                }

                if (target.RecitedAt.HasValue)
                {
                    return new OperationResult(false, "이미 암송 처리된 말씀입니다.");
                }

                target.UsedAt = DateTime.UtcNow;
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return new OperationResult(true, "말씀을 사용 처리했습니다.");
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<OperationResult> ReciteVerseAsync(Guid groupId, Guid verseId)
        {
            await _mutex.WaitAsync();
            try
            {
                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                var target = registrations.FirstOrDefault(r => r.GroupId == groupId && r.VerseId == verseId);
                if (target == null)
                {
                    return new OperationResult(false, "등록된 말씀을 찾을 수 없습니다.");
                }

                if (target.UsedAt.HasValue)
                {
                    return new OperationResult(false, "이미 사용된 말씀입니다.");
                }

                if (target.RecitedAt.HasValue)
                {
                    return new OperationResult(false, "이미 암송 처리된 말씀입니다.");
                }

                target.RecitedAt = DateTime.UtcNow;
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return new OperationResult(true, "말씀을 암송 처리했습니다.");
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<IReadOnlyList<string>> GetVersePurposesAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                await EnsureDefaultPurposesAsync();
                var purposes = await ReadListInternalAsync<string>(PurposesFileName);
                return purposes;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> AddVersePurposeAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            await _mutex.WaitAsync();
            try
            {
                await EnsureDefaultPurposesAsync();
                var purposes = await ReadListInternalAsync<string>(PurposesFileName);
                if (purposes.Any(p => string.Equals(p, name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                purposes.Add(name.Trim());
                await WriteListInternalAsync(PurposesFileName, purposes);
                return true;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<OperationResult> ResetVerseStatusAsync(Guid groupId, Guid verseId)
        {
            await _mutex.WaitAsync();
            try
            {
                var registrations = await ReadListInternalAsync<Registration>(RegistrationsFileName);
                var target = registrations.FirstOrDefault(r => r.GroupId == groupId && r.VerseId == verseId);
                if (target == null)
                {
                    return new OperationResult(false, "등록된 말씀을 찾을 수 없습니다.");
                }

                if (!target.UsedAt.HasValue && !target.RecitedAt.HasValue)
                {
                    return new OperationResult(false, "되돌릴 상태가 없습니다.");
                }

                target.UsedAt = null;
                target.RecitedAt = null;
                await WriteListInternalAsync(RegistrationsFileName, registrations);
                return new OperationResult(true, "상태를 되돌렸습니다.");
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<IReadOnlyList<T>> ReadListAsync<T>(string fileName)
        {
            await _mutex.WaitAsync();
            try
            {
                var list = await ReadListInternalAsync<T>(fileName);
                return list;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<List<T>> ReadListInternalAsync<T>(string fileName)
        {
            var path = GetDataPath(fileName);
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(path);
            var list = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions);
            return list ?? new List<T>();
        }

        private async Task WriteListInternalAsync<T>(string fileName, List<T> items)
        {
            var path = GetDataPath(fileName);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
        }

        private string GetDataPath(string fileName)
        {
            return Path.Combine(_dataRoot, fileName);
        }

        private async Task EnsureDefaultPurposesAsync()
        {
            var purposes = await ReadListInternalAsync<string>(PurposesFileName);
            if (purposes.Count > 0)
            {
                return;
            }

            purposes = new List<string> { DefaultMealPurpose, DefaultSnackPurpose };
            await WriteListInternalAsync(PurposesFileName, purposes);
        }

        private static string HashPassword(Guid groupId, string password)
        {
            var normalized = password.Trim();
            var input = $"{groupId:N}:{normalized}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private sealed class VerseJsonConverter : JsonConverter<Verse>
        {
            public override Verse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                var root = document.RootElement;

                var id = root.TryGetProperty("id", out var idElement) ? idElement.GetGuid() : Guid.Empty;
                var text = root.TryGetProperty("text", out var textElement) ? textElement.GetString() ?? string.Empty : string.Empty;
                var typeValue = DefaultMealPurpose;

                if (root.TryGetProperty("type", out var typeElement))
                {
                    switch (typeElement.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (typeElement.TryGetInt32(out var numericType))
                            {
                                typeValue = numericType == 1 ? DefaultSnackPurpose : DefaultMealPurpose;
                            }
                            break;
                        case JsonValueKind.String:
                            typeValue = typeElement.GetString() ?? DefaultMealPurpose;
                            break;
                    }
                }

                return new Verse
                {
                    Id = id,
                    Text = text,
                    Type = typeValue
                };
            }

            public override void Write(Utf8JsonWriter writer, Verse value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("id", value.Id);
                writer.WriteString("text", value.Text);
                writer.WriteString("type", value.Type);
                writer.WriteEndObject();
            }
        }
    }
}
