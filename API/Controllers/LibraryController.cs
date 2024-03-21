using API.Data;
using API.Entities;
using API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        private readonly Context _context;
        private readonly EmailService _emailService;

        // Constructor
        public LibraryController(Context context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [Authorize]
        [HttpGet("GetAllBooks")]
        public async Task<ActionResult<IEnumerable<Book>>> GetAllBooks()
        {
            try
            {
                var allBooks = await _context.Books.ToListAsync();
                return Ok(allBooks);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [Authorize]
        [HttpPost("OrderBook")]
        public async Task<ActionResult> OrderBook(int userId, int bookId)
        {
            var canOrder = _context.Orders.Count(o => o.UserId == userId && !o.Returned) < 3;

            if (canOrder)
            {
                _context.Orders.Add(new()
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    ReturnDate = null,
                    Returned = false,
                    FinePaid = 0
                });

                var book = _context.Books.Find(bookId);
                if (book is not null)
                {
                    book.Ordered = true;
                }

                await _context.SaveChangesAsync();
                return Ok("ORDERED");
            }
            return Ok("CANNOT ORDER");
        }

        [Authorize]
        [HttpGet("GetOrdersOFUser")]
        public async Task<ActionResult> GetOrdersOFUser(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if (orders.Any()) { return Ok(orders); }
            else { return NotFound(); }
        }

        [Authorize]
        [HttpPost("AddCategory")]
        public async Task<ActionResult> AddCategory(BookCategory bookCategory)
        {
            var exists = _context.BookCategories.Any(bc => bc.Category == bookCategory.Category && bc.SubCategory == bookCategory.SubCategory);
            if (exists)
            {
                return Ok("CANNOT INSERT");
            }
            else
            {
                _context.BookCategories.Add(new()
                {
                    Category = bookCategory.Category,
                    SubCategory = bookCategory.SubCategory
                });
                await _context.SaveChangesAsync();

                return Ok("CREATED");
            }
        }

        [Authorize]
        [HttpGet("GetAllCategories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetAllCategories()
        {
            try
            {
                var allCategories = await _context.BookCategories.ToListAsync();
                return Ok(allCategories);
            }
            catch (Exception)
            {
                throw;
            }   
        }

        [Authorize]
        [HttpPost("AddBook")]
        public async Task<ActionResult> AddBook([FromBody] Book book)
        {
            try
            {
                book.BookCategory = null;
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                return Ok("CREATED");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpDelete("DeleteBook")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            try
            {
                var exists = _context.Books.Any(b => b.Id == id);
                if (exists)
                {
                    var book = _context.Books.Find(id);
                    _context.Books.Remove(book!);
                    await _context.SaveChangesAsync();
                    return Ok("DELETED");
                }
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet("ReturnBook")]
        public async Task<ActionResult> ReturnBook(int userId, int bookId, int fine)
        {
            try
            {
                var order = _context.Orders.SingleOrDefault(o => o.UserId == userId && o.BookId == bookId);
                if (order is not null)
                {
                    order.Returned = true;
                    order.ReturnDate = DateTime.Now;
                    order.FinePaid = fine;

                    var book = _context.Books.Single(b => b.Id == order.BookId);
                    book.Ordered = false;

                    await _context.SaveChangesAsync();

                    return Ok("RETURNED");
                }
                return Ok("NOT RETURNED");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            try
            {
                var allOrders =await _context.Orders.ToListAsync();
                return Ok(allOrders);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet("SendEmailForPendingReturns")]
        public async Task<ActionResult> SendEmailForPendingReturns()
        {
            try
            {
                var orders = await _context.Orders
                            .Include(o => o.Book)
                            .Include(o => o.User)
                            .Where(o => !o.Returned)
                            .ToListAsync();

                var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
                emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

                var firstFineEmails = emailsWithFine.Where(x => x.FinePaid == 50).ToList();
                firstFineEmails.ForEach(x =>
                {
                    var body = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>Yesterday was your last day to return Book: "{x.Book?.Title}".</h4>
                        <h4>From today, every day a fine of 50Rs will be added for this book.</h4>
                        <h4>Please return it as soon as possible.</h4>
                        <h4>If your fine exceeds 500Rs, your account will be blocked.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                    _emailService.SendEmail(x.User!.Email, "RETURN OVERDUE", body);
                });

                var regularFineEmails = emailsWithFine.Where(x => x.FinePaid > 50 && x.FinePaid <= 500).ToList();
                regularFineEmails.ForEach(x =>
                {
                    var regularFineEmailsBody = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Pleae pay it as soon as possible.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                    _emailService.SendEmail(x.User?.Email!, "FINE TO PAY", regularFineEmailsBody);
                });

                var overdueFineEmails = emailsWithFine.Where(x => x.FinePaid > 500).ToList();
                overdueFineEmails.ForEach(x =>
                {
                    var overdueFineEmailsBody = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Your account is BLOCKED.</h4>
                        <h4>Pleae pay it as soon as possible to UNBLOCK your account.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                    _emailService.SendEmail(x.User?.Email!, "Fine Overdue", overdueFineEmailsBody);
                });

                return Ok("SENT");
            }
            catch (Exception)
            {
                throw;
            }            
        }

        [Authorize]
        [HttpGet("BlockFineOverdueUsers")]
        public async Task<ActionResult> BlockFineOverdueUsers()
        {
            try
            {
                var orders = await _context.Orders
                            .Include(o => o.Book)
                            .Include(o => o.User)
                            .Where(o => !o.Returned)
                            .ToListAsync();

                var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
                emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

                var users = emailsWithFine.Where(x => x.FinePaid > 500).Select(x => x.User!).Distinct().ToList();

                if (users is not null && users.Any())
                {
                    foreach (var user in users)
                    {
                        user.AccountStatus = AccountStatus.BLOCKED;
                    }
                    await _context.SaveChangesAsync();

                    return Ok("BLOCKED");
                }
                else
                {
                    return Ok("NOT BLOCKED");
                }
            }
            catch (Exception)
            {
                throw;
            }            
        }

        [Authorize]
        [HttpGet("Unblock")]
        public async Task<ActionResult> Unblock(int userId)
        {
            var user = _context.Users.Find(userId);
            if(user is not null)
            {
                user.AccountStatus = AccountStatus.ACTIVE;
                await _context.SaveChangesAsync();
                return Ok("UNBLOCKED");
            }
            return Ok("NOT UNBLOCKED");
        }
    }
}
