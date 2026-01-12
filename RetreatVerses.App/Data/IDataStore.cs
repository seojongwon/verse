using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetreatVerses.App.Data
{
    public interface IDataStore
    {
        Task<IReadOnlyList<Group>> GetGroupsAsync();
        Task<Group> AddGroupAsync(string name, string password);
        Task<bool> UpdateGroupAsync(Guid id, string name, string? password);
        Task<bool> VerifyGroupPasswordAsync(Guid groupId, string password);
        Task<bool> DeleteGroupAsync(Guid id);
        Task<int> DeleteGroupsAsync(IEnumerable<Guid> ids);
        Task<int> DeleteAllGroupsAsync();

        Task<IReadOnlyList<Verse>> GetVersesAsync();
        Task<Verse> AddVerseAsync(string text, string type);
        Task<bool> UpdateVerseAsync(Guid id, string text, string type);
        Task<bool> DeleteVerseAsync(Guid id);
        Task<int> DeleteVersesAsync(IEnumerable<Guid> ids);
        Task<int> DeleteAllVersesAsync();
        Task<IReadOnlyList<string>> GetVersePurposesAsync();
        Task<bool> AddVersePurposeAsync(string name);

        Task<IReadOnlyList<Registration>> GetRegistrationsAsync();
        Task<IReadOnlyList<Registration>> GetRegistrationsForGroupAsync(Guid groupId);
        Task<OperationResult> RegisterVerseAsync(Guid groupId, Guid verseId);
        Task<OperationResult> UnregisterVerseAsync(Guid groupId, Guid verseId);
        Task<OperationResult> UseVerseAsync(Guid groupId, Guid verseId);
        Task<OperationResult> ReciteVerseAsync(Guid groupId, Guid verseId);
        Task<OperationResult> ResetVerseStatusAsync(Guid groupId, Guid verseId);
    }
}
