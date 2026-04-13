using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;

namespace WebApplication1.Services
{
    public class UserDomainService
    {
        private readonly IUserRepository _repo;

        public UserDomainService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task CheckEmailUnique(string email, Guid? excludeId = null)
        {
            var exists = await _repo.EmailExistsAsync(email, excludeId);

            if (exists)
                throw new Exception($"Invalid Email '{email}'.");
        }

        public async Task<User> CheckUserExistAndGet(Guid targetUserId, Guid? currentUserId = null)
        {
            var user = await _repo.GetByIdAsync(targetUserId);

            if (user == null)
                throw new Exception($"User with id {targetUserId} not found.");

            if (currentUserId.HasValue && targetUserId == currentUserId.Value)
                throw new Exception("Cannot perform this operation on yourself.");

            return user;
        }
    }
}
