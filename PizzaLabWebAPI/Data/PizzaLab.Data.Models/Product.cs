﻿namespace PizzaLab.Data.Models
{
    using Common;
    using System.Collections.Generic;

    public class Product : BaseModel<string>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Weight { get; set; }

        public string Image { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; }

        public ICollection<Ingredient> Ingredients { get; set; }

        public ICollection<ApplicationUser> Likes { get; set; }

        public ICollection<Review> Reviews { get; set; }
    }
}
