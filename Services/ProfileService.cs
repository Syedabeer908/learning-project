using WebApplication1.Entities;
using WebApplication1.Interfaces;
using WebApplication1.Common.Exceptions;

namespace WebApplication1.Services
{
    public class ProfileService
    {
        private readonly FileService _fileService;
        private readonly IUserRepository _repo;

        public ProfileService(FileService fileService, IUserRepository repo)
        {
            _fileService = fileService;
            _repo = repo;
        }

        public async Task<User> CheckUserExistAndGet(Guid targetUserId)
        {
            var user = await _repo.GetByIdAsync(targetUserId);

            if (user == null)
                throw new NotFoundException($"User with id {targetUserId} not found.");
            return user;
        }

        public async Task<string> UpdateProfileImage(Guid userId, IFormFile file)
        {
            var user = await CheckUserExistAndGet(userId);

            var folderPath = "users/profile/images/" + userId;
            var imagePath = await _fileService.UploadFile(file, folderPath);

            await UpdateProfile(user, imagePath);

            return imagePath;
        }

        public async Task<string?> GetProfile(Guid userId)
        {
            var user = await CheckUserExistAndGet(userId);
            return user.ProfileImagePath;
        }

        public async Task UpdateProfile(User user, string? filepath = null )
        {
            if (user == null )
                return;

            if(user.ProfileImagePath != null)
            {
                await _fileService.DeleteFile(user.ProfileImagePath);
            }

            user.ProfileImagePath = filepath;
            await _repo.UpdateAsync(user);
        }
    }
}
