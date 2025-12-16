using System.Threading.Tasks;

namespace duAnLUAGA.Application.Interfaces
{
    public interface IChatService
    {
        Task<string> GetReplyAsync(string userMessage, string context = "");
    }
}