using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MyHotel.Models;
using System.Data;
using NuGet.Versioning;

namespace MyHotel.Controllers
{
    public class HotelController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public HotelController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        HotelwebContext dc= new HotelwebContext();
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Home()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(Customer r)
        {
            var res = dc.Customers.Where(c => c.Email == r.Email && c.Password == r.Password).FirstOrDefault();
            if (res != null)
            {



                List<Claim> li = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,r.Email),
                    new Claim(ClaimTypes.Role,r.Email),
                    new Claim(ClaimTypes.Sid,res.Customerid.ToString())
                };
                var identity = new ClaimsIdentity(li, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var login = HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                string role = User.FindFirstValue(ClaimTypes.Role);
                if (res.Email == "admin@gmail.com")
                {
                    return RedirectToAction("display");
                }
                else
                {
                    return RedirectToAction("Home");
                }

            }
            else
            {
                ViewData["msg"] = "invalid user";
            }



            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(Customer r)
        {



            if (ModelState.IsValid)
            {



                dc.Customers.Add(r);
                var rowaffect = dc.SaveChanges();



                //var rowaffect = dc.Add<Customer>(r);



                if (rowaffect > 0)
                {
                    return RedirectToAction("Login");

                }
                else
                {
                    ViewData["a"] = "Error occured pls try again";
                }
            }



            return View();



        }
        public IActionResult Rooms()
        {
            var res = from t in dc.Rooms
                      where t.Available == true
                      select t;
            return View(res);




        }
        public IActionResult Booking()
        {




            if (User.Identity.IsAuthenticated)
            {
                ViewData["roomid"] = Request.Query["roomid"];
                ViewData["roomimage"] = Request.Query["roomimage"];
                ViewData["roomprice"] = Request.Query["roomprice"];
                ViewData["roomtype"] = Request.Query["roomtype"];
                ViewData["roomcapacity"] = Request.Query["roomcapacity"];
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }



        [HttpPost]
        public IActionResult Booking(Roombooking o)
        {
            var roomres = (from r in dc.Rooms
                           where r.Roomid == o.Roomid
                           select r).FirstOrDefault();

            int custid = int.Parse(User.FindFirstValue(ClaimTypes.Sid));

            Roombooking room = new Roombooking()
            {
                Customerid = custid,
                Roomid = o.Roomid,
                Bookingfrom = o.Bookingfrom,
                BookingTo = o.BookingTo
            };




            


            DateTime FromYear = Convert.ToDateTime(o.Bookingfrom);
            DateTime ToYear = Convert.ToDateTime(o.BookingTo);
            TimeSpan objTimeSpan = ToYear - FromYear;
            int Days = Convert.ToInt32(objTimeSpan.TotalDays);


            roomres.Available = false;
            //dc.Update(roomres);

            var acc = dc.BankAccounts.Where(a => a.Customerid == custid).FirstOrDefault();
            int roomPrice = (int)dc.Rooms.Where(a => a.Roomid == o.Roomid).FirstOrDefault().Roomprice;


            if (debitFromBankAccount(custid, (int)o.Roomid, Days))
            {
                dc.Add(room);
                dc.Rooms.Update(roomres);
                dc.SaveChanges();

                decimal roomPrice2 = (decimal)dc.Rooms.Where(a => a.Roomid == o.Roomid).FirstOrDefault().Roomprice;
                decimal totalRent = roomPrice2 * Days;

                Payment pay = new Payment();
                pay.Customerid = custid;
                pay.Bookingid = room.Bookingid;
                pay.Paymentamt = totalRent;
                dc.Add(pay);
                dc.SaveChanges();

                ViewData["r"] = "Booking Successful";
            }
            else
            {
                ViewData["r"] = "Booking unsuccessful " + TempData["error"];
            }



            return RedirectToAction("Rooms");



        }

        [NonAction]
        public bool debitFromBankAccount(int userid, int roomid, int days)
        {
            var res = dc.BankAccounts.Where(a => a.Customerid == userid).FirstOrDefault();
            decimal roomPrice = (decimal)dc.Rooms.Where(a => a.Roomid == roomid).FirstOrDefault().Roomprice;
            decimal totalRent = roomPrice * days;
            if (res.Balance >= totalRent)
            {
                res.Balance -= roomPrice;
                dc.Update(res);
                int no = dc.SaveChanges();
                if (no > 0)
                    return true;
            }
            TempData["error"] = "Low balance";
            return false;
        }







        public IActionResult Logout()
        {
            var login = HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        public IActionResult Search()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Search(string ser)
        {
            var res = from t in dc.Rooms
                      where t.Roomtype == ser
                      select t;


            return View(res);
        }
        [Authorize(Roles = "admin@gmail.com")]
        public IActionResult add()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> add(Room b)
        {
            if (b.roomimage != null)
            {
                string folder = "images/";
                b.Roomimage = await imageupload(folder, b.roomimage);

            }




            await dc.Rooms.AddAsync(b);
            int i=await dc.SaveChangesAsync();

            if (i > 0)
            {
                ViewData["r"] = "Room Added  successfully";
            }

            return View();
        }
        public async Task<string> imageupload(string folderPath, IFormFile file)
        {
            folderPath += Guid.NewGuid().ToString() + "_" + file.FileName;
            string severFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderPath);
            await file.CopyToAsync(new FileStream(severFolder, FileMode.Create));
            return "/" + folderPath;
        }
        [Authorize(Roles = "admin@gmail.com")]
        public IActionResult display()
        {
            var res = from t in dc.Rooms
                      select t;

            return View(res);

        }
        [Authorize(Roles = "admin@gmail.com")]
        public IActionResult delete()
        {
            int s = Convert.ToInt32(Request.Query["roomid"]);
            var res = dc.Rooms.Where(t => t.Roomid == s).FirstOrDefault();

            if (res != null)
            {
                dc.Rooms.Remove(res);
                dc.SaveChanges();
                ViewData["msg"] = "Deleted successfully";
            }
            else
            {
                ViewData["msg"] = "Not Deleted!";
            }







            return RedirectToAction("display");

        }
        [Authorize(Roles = "admin@gmail.com")]
        public IActionResult Bookingdisplay()
        {
            var res = from r in dc.Roombookings
                      from p in dc.Payments
                      from c in dc.Customers
                      where p.Customerid == c.Customerid && p.Bookingid == r.Bookingid
                      select new ViewBookings { customer=c, payment=p, roombooking=r };


            return View(res);

        }
        [Authorize(Roles = "admin@gmail.com")]
        public IActionResult Custdetails()
        {
            var res = from t in dc.Customers
                      select t;
            return View(res);
        }
        public IActionResult Userdisplay()
        {
            if (User.Identity.IsAuthenticated)
            {
                int custid = int.Parse(User.FindFirstValue(ClaimTypes.Sid));
                var res = from r in dc.Roombookings
                          join s1 in dc.Customers on r.Customerid equals s1.Customerid
                          join s2 in dc.Payments on r.Bookingid equals s2.Bookingid
                          join c in dc.Rooms on r. Roomid equals c.Roomid
                          where r.Customerid == custid
                          select new Userbooking { roombooking = r, customer = s1,  room = c ,payment=s2};
             

                return View(res);

               
            }
            else
            {
                return RedirectToAction("Login");
            }
            

            

        }
        public IActionResult Contactus()
        {
            return View();
        }
    }
}
