using System.ComponentModel.DataAnnotations.Schema;

namespace ShopApp.Models
{
    public partial class User
    {
        [NotMapped]
        public string FullName => $"{Lastname} {Name} {Midname}".Trim();
    }
}
