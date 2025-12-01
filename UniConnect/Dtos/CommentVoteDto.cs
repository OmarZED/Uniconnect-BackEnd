using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class CommentVoteDto
    {
        public string Id { get; set; }
        public string CommentId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public VoteType VoteType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddCommentVoteDto
    {
        [Required]
        public VoteType VoteType { get; set; }
    }
}
