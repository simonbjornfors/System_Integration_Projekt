using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApi.Model
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Default value
        public string Author { get; set; } = string.Empty; // Default value
        public bool IsBorrowed { get; set; } = false;
    }
}