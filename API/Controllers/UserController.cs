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
    public class UserController : ControllerBase
    {
        private readonly Context _context;
        private readonly EmailService _emailService;
        private readonly JwtService _jwtService;

        public UserController(Context context, EmailService emailService, JwtService jwtService)
        {
            _context = context;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                var allUsers = await _context.Users.ToListAsync();
                return Ok(allUsers);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET api/<UserController>/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                var singleUser = await _context.Users.SingleOrDefaultAsync(x => x.Id == id);

                if (singleUser == null) { return NotFound(); }
                
                return Ok(singleUser);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // POST api/<UserController>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetUser), new { id = user.Id  }, user);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // PUT api/<UserController>/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) { return NotFound(); }
                else { throw; }
            }
            return Accepted();
        }

        // DELETE api/<UserController>/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DelUser(int id)
        {
            try
            {
                var delUser = await _context.Users.FindAsync(id);
                if (delUser == null) { return NotFound(); }

                _context.Users.Remove(delUser);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet("ApproveRequest")]
        public async Task<ActionResult> ApproveRequest(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);

                if (user is not null)
                {
                    if (user.AccountStatus == AccountStatus.UNAPROOVED)
                    {
                        user.AccountStatus = AccountStatus.ACTIVE;
                        await _context.SaveChangesAsync();

                        _emailService.SendEmail(user.Email, "Account Approved", $"""
                        <html>
                            <body>
                                <h2>Hi, {user.FirstName} {user.LastName}</h2>
                                <h3>You Account has been approved by admin.</h3>
                                <h3>Now you can login to your account.</h3>
                            </body>
                        </html>
                    """);
                        return Ok("APPROVED");
                    }
                }

                return Ok("NOT APPROVED");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register(User user)
        {
            user.AccountStatus = AccountStatus.UNAPROOVED;
            user.UserType = UserType.STUDENT;
            user.CreatedOn = DateTime.Now;

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                const string subject = "Account Created";
                var body = $"""
                                    <html>
                                        <body>
                                            <h1>Hello, {user.FirstName} {user.LastName}</h1>
                                            <h2>
                                                Your account has been created and we have sent approval request to admin. 
                                                Once the request is approved by admin you will receive email, and you will be
                                                able to login in to your account.
                                            </h2>
                                            <h3>Thanks</h3>
                                        </body>
                                    </html>
                                    """;
                _emailService.SendEmail(user.Email, subject, body);

                return Ok(@"Thank you for registering. 
                        Your account has been sent for aprooval. 
                        Once it is aprooved, you will get an email.");
            }
            catch (Exception)
            {
                throw;
            }            
        }

        [HttpPost("Login")]
        public ActionResult Login(string? email, string? password)
        {
            try
            {
                if (_context.Users.Any(u => u.Email!.Equals(email) && u.Password!.Equals(password)))
                {
                    var user = _context.Users.Single(user => user.Email!.Equals(email) && user.Password!.Equals(password));

                    if (user.AccountStatus == AccountStatus.UNAPROOVED) { return Ok("UNAPPROVED"); }
                    if (user.AccountStatus == AccountStatus.BLOCKED) { return Ok("BLOCKED"); }

                    return Ok(_jwtService.GenerateToken(user));
                }
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Additional Methods
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
