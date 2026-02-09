using System.Threading.Tasks;

namespace RetreatVerses.App.Data
{
    public interface IMorphologyService
    {
        Task<MorphologyResult> CheckNounAsync(string word);
    }
}
