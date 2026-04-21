using WebApplication1.DTOs;

namespace WebApplication1.Mappers
{
    public class ProfileImageMapper
    {
        public GetProfileImageDto ToDto(string url)
        {
            return new GetProfileImageDto
            {
                ProfileImageUrl = url
            };
        }
    }
}
