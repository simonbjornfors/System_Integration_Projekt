using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApi.Model
{
    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string? Borrower { get; set; }
        public DateTime? LoanDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }
}