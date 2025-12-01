using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class PostReactionDto
    {
        public string Id { get; set; }
        public string PostId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public ReactionType ReactionType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddPostReactionDto
    {
        [Required]
        public ReactionType ReactionType { get; set; }
    }
}
