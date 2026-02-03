using System;
using System.Collections.Generic;

namespace ShopApp.Models;

public partial class Image
{
    public int Id { get; set; }

    public string? Image1 { get; set; }

    public int ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;
}
